#!/usr/bin/env bash
set -euo pipefail

readonly queue_threshold_seconds=300
readonly sample_interval_seconds=60
readonly observation_timeout_seconds=20

usage() {
  cat <<'EOF'
Usage:
  tools/runner-doctor.sh --repo OWNER/REPO --runner NAME --job POSITIVE_ID \
    --ssh USER@HOST --service NAME.service [--fixture FILE]

Runs exactly two bounded, read-only observations. Live mode uses the operator's
existing gh authentication and SSH key, waiting 60 seconds between samples.
Fixture mode reads two strict SAMPLE1_*/SAMPLE2_* KEY=VALUE samples without
calling gh or ssh and without sleeping. All identity arguments are mandatory.

Exit codes:
  0  HEALTHY_IDLE or HEALTHY_BUSY
  1  SUSPECT_DISPATCH (manual recovery review required)
  2  OFFLINE
  3  UNKNOWN / observation failure
  8  QUEUE_GRACE
 64  CLI or fixture usage error
EOF
}

die() {
  printf 'runner-doctor: %s\n' "$1" >&2
  exit 64
}

require_value() {
  [[ -n "${2:-}" && "${2:-}" != --* ]] || die "missing value for $1"
}

repo=""
runner_name=""
job_id=""
ssh_target=""
service_name=""
fixture_path=""
repo_owner=""
repo_name=""

while (($# > 0)); do
  case "$1" in
    --repo|--runner|--job|--ssh|--service|--fixture)
      require_value "$1" "${2:-}"
      case "$1" in
        --repo) repo="$2" ;;
        --runner) runner_name="$2" ;;
        --job) job_id="$2" ;;
        --ssh) ssh_target="$2" ;;
        --service) service_name="$2" ;;
        --fixture) fixture_path="$2" ;;
      esac
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *) die "unknown argument: $1" ;;
  esac
done

[[ "$repo" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]] || die "unsafe --repo"
repo_owner="${repo%%/*}"
repo_name="${repo#*/}"
[[ "$repo_owner" != "." && "$repo_owner" != ".." && "$repo_name" != "." && "$repo_name" != ".." ]] || die "unsafe --repo"
[[ "$runner_name" =~ ^[A-Za-z0-9._-]+$ && "$runner_name" != -* ]] || die "unsafe --runner"
[[ "$job_id" =~ ^[1-9][0-9]*$ ]] || die "unsafe --job"
[[ "$ssh_target" =~ ^[A-Za-z0-9._-]+@[A-Za-z0-9._:-]+$ \
  && "$ssh_target" != -* \
  && "${ssh_target#*@}" != -* ]] || die "unsafe --ssh"
[[ "$service_name" =~ ^[A-Za-z0-9_.@-]+\.service$ && "$service_name" != -* ]] || die "unsafe --service"

normalize_labels() {
  tr '[:upper:]' '[:lower:]' <<< "$1" | tr ',' '\n' | LC_ALL=C sort -u | paste -sd, -
}

labels_compatible() {
  local available=",$(normalize_labels "$1"),"
  local required
  while IFS= read -r required; do
    [[ -n "$required" ]] || continue
    [[ "$available" == *",${required},"* ]] || return 1
  done < <(tr ',' '\n' <<< "$(normalize_labels "$2")")
}

rfc3339_epoch() {
  local value="$1"
  local normalized
  [[ "$value" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z$ ]] || return 1
  normalized="$(date -u -d "$value" +%Y-%m-%dT%H:%M:%SZ 2>/dev/null)" || return 1
  [[ "$normalized" == "$value" ]] || return 1
  date -u -d "$value" +%s
}

init_sample() {
  local -n sample="$1"
  sample[at]=""
  sample[at_epoch]="unknown"
  sample[api_ok]="false"
  sample[ssh_ok]="false"
  sample[runner_id]="unknown"
  sample[runner_status]="unknown"
  sample[runner_busy]="unknown"
  sample[runner_labels]="unknown"
  sample[job_status]="unknown"
  sample[job_created_at]=""
  sample[job_started_at]=""
  sample[job_started_at_interpretation]="unavailable"
  sample[job_labels]="unknown"
  sample[service_active]="unknown"
  sample[listener_count]="unknown"
  sample[worker_count]="unknown"
  sample[queue_age_source]="unavailable"
  sample[queue_age_seconds]="unknown"
  sample[error]="none"
}

set_error() {
  local -n sample="$1"
  if [[ "${sample[error]}" == "none" ]]; then
    sample[error]="$2"
  fi
}

finalize_sample() {
  local -n sample="$1"
  local created_epoch started_epoch

  if ! sample[at_epoch]="$(rfc3339_epoch "${sample[at]}")"; then
    sample[at_epoch]="unknown"
    set_error "$1" "sample_at_invalid"
  fi

  if [[ "${sample[api_ok]}" != "true" ]]; then
    set_error "$1" "api_observation_failed"
  fi
  if [[ "${sample[ssh_ok]}" != "true" ]]; then
    set_error "$1" "ssh_observation_failed"
  fi

  if ! created_epoch="$(rfc3339_epoch "${sample[job_created_at]}")"; then
    set_error "$1" "job_created_at_invalid"
    return
  fi
  if [[ "${sample[at_epoch]}" =~ ^[0-9]+$ && "$created_epoch" -gt "${sample[at_epoch]}" ]]; then
    set_error "$1" "job_created_at_future"
    return
  fi

  if [[ "${sample[job_status]}" == "queued" ]]; then
    if [[ -z "${sample[job_started_at]}" ]]; then
      sample[job_started_at_interpretation]="queued_empty"
    elif [[ "${sample[job_started_at]}" == "${sample[job_created_at]}" ]]; then
      sample[job_started_at_interpretation]="queued_created_at_sentinel"
    else
      sample[job_started_at_interpretation]="queued_mismatch"
      set_error "$1" "queued_job_started_at_mismatch"
      return
    fi
    if [[ "${sample[at_epoch]}" =~ ^[0-9]+$ ]]; then
      sample[queue_age_source]="job.created_at"
      sample[queue_age_seconds]="$((sample[at_epoch] - created_epoch))"
    fi
  else
    if [[ -z "${sample[job_started_at]}" ]] || ! started_epoch="$(rfc3339_epoch "${sample[job_started_at]}")"; then
      set_error "$1" "nonqueued_job_started_at_invalid"
      return
    fi
    if ((started_epoch < created_epoch)) || { [[ "${sample[at_epoch]}" =~ ^[0-9]+$ ]] && ((started_epoch > sample[at_epoch])); }; then
      set_error "$1" "job_time_order_invalid"
      return
    fi
    sample[job_started_at_interpretation]="actual_start"
    sample[queue_age_source]="not_queued"
    sample[queue_age_seconds]="0"
  fi
}

collect_live_sample() {
  local -n sample="$1"
  local runner_list runner_json job_json remote_output remote_command
  local runner_id runner_status runner_busy runner_labels
  local observed_job_id job_status job_created_at job_started_at job_labels
  init_sample "$1"
  sample[at]="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

  if runner_list="$(timeout "${observation_timeout_seconds}s" gh api "repos/${repo}/actions/runners?per_page=100" 2>/dev/null)" \
    && job_json="$(timeout "${observation_timeout_seconds}s" gh api "repos/${repo}/actions/jobs/${job_id}" 2>/dev/null)"; then
    if ! runner_json="$(jq -ce --arg name "$runner_name" '
      if (.runners | type) == "array" then
        [.runners[] | select(.name == $name)] |
        if length == 1 then .[0] else empty end
      else
        empty
      end
    ' <<< "$runner_list" 2>/dev/null)"; then
      set_error "$1" "runner_response_invalid_or_runner_not_found"
    elif runner_id="$(jq -er '.id | select(type == "number" and . > 0 and floor == .) | tostring' <<< "$runner_json" 2>/dev/null)" \
      && runner_status="$(jq -er '.status | select(. == "online" or . == "offline")' <<< "$runner_json" 2>/dev/null)" \
      && runner_busy="$(jq -er 'if .busy == true then "true" elif .busy == false then "false" else empty end' <<< "$runner_json" 2>/dev/null)" \
      && runner_labels="$(jq -er '
        if (.labels | type) == "array"
          and (.labels | length) > 0
          and all(.labels[]; (.name | type) == "string" and (.name | test("^[A-Za-z0-9._-]+$")))
        then [.labels[].name] | join(",")
        else empty
        end
      ' <<< "$runner_json" 2>/dev/null)" \
      && observed_job_id="$(jq -er '.id | select(type == "number" and . > 0 and floor == .) | tostring' <<< "$job_json" 2>/dev/null)" \
      && job_status="$(jq -er '.status | select(. == "queued" or . == "in_progress" or . == "completed")' <<< "$job_json" 2>/dev/null)" \
      && job_created_at="$(jq -er '.created_at | select(type == "string")' <<< "$job_json" 2>/dev/null)" \
      && job_started_at="$(jq -er 'if .started_at == null then "" elif (.started_at | type) == "string" then .started_at else empty end' <<< "$job_json" 2>/dev/null)" \
      && job_labels="$(jq -er '
        if (.labels | type) == "array"
          and (.labels | length) > 0
          and all(.labels[]; type == "string" and test("^[A-Za-z0-9._-]+$"))
        then .labels | join(",")
        else empty
        end
      ' <<< "$job_json" 2>/dev/null)" \
      && [[ "$observed_job_id" == "$job_id" ]]; then
      sample[api_ok]="true"
      sample[runner_id]="$runner_id"
      sample[runner_status]="$runner_status"
      sample[runner_busy]="$runner_busy"
      sample[runner_labels]="$runner_labels"
      sample[job_status]="$job_status"
      sample[job_created_at]="$job_created_at"
      sample[job_started_at]="$job_started_at"
      sample[job_labels]="$job_labels"
    else
      set_error "$1" "github_api_payload_invalid"
    fi
  else
    set_error "$1" "github_api_failed"
  fi

  remote_command="service_state=\$(systemctl is-active -- '${service_name}' 2>/dev/null); if [ \"\$service_state\" = active ]; then service_active=true; else service_active=false; fi; if command -v pgrep >/dev/null 2>&1; then listener_count=\$(pgrep -fc '[R]unner.Listener' 2>/dev/null || :); worker_count=\$(pgrep -fc '[R]unner.Worker' 2>/dev/null || :); printf 'SERVICE_ACTIVE=%s\\nLISTENER_COUNT=%s\\nWORKER_COUNT=%s\\n' \"\$service_active\" \"\$listener_count\" \"\$worker_count\"; else exit 70; fi"
  if remote_output="$(timeout "${observation_timeout_seconds}s" ssh -o BatchMode=yes -o ConnectTimeout=10 -- "$ssh_target" "$remote_command" 2>/dev/null)"; then
    sample[service_active]="$(sed -n 's/^SERVICE_ACTIVE=//p' <<< "$remote_output" | sed -n '1p')"
    sample[listener_count]="$(sed -n 's/^LISTENER_COUNT=//p' <<< "$remote_output" | sed -n '1p')"
    sample[worker_count]="$(sed -n 's/^WORKER_COUNT=//p' <<< "$remote_output" | sed -n '1p')"
    if [[ "${sample[service_active]}" =~ ^(true|false)$ && "${sample[listener_count]}" =~ ^[0-9]+$ && "${sample[worker_count]}" =~ ^[0-9]+$ ]]; then
      sample[ssh_ok]="true"
    else
      set_error "$1" "ssh_output_invalid"
    fi
  else
    set_error "$1" "ssh_probe_failed"
  fi

  finalize_sample "$1"
}

readonly -a fixture_fields=(
  AT API_OK SSH_OK RUNNER_STATUS RUNNER_BUSY RUNNER_LABELS JOB_STATUS
  JOB_CREATED_AT JOB_STARTED_AT JOB_LABELS SERVICE_ACTIVE LISTENER_COUNT WORKER_COUNT
)

validate_fixture_scalar() {
  local field="$1"
  local value="$2"
  case "$field" in
    AT|JOB_CREATED_AT) rfc3339_epoch "$value" >/dev/null ;;
    JOB_STARTED_AT) [[ -z "$value" ]] || rfc3339_epoch "$value" >/dev/null ;;
    API_OK|SSH_OK|RUNNER_BUSY|SERVICE_ACTIVE) [[ "$value" =~ ^(true|false)$ ]] ;;
    RUNNER_STATUS) [[ "$value" =~ ^(online|offline)$ ]] ;;
    JOB_STATUS) [[ "$value" =~ ^(queued|in_progress|completed)$ ]] ;;
    LISTENER_COUNT|WORKER_COUNT) [[ "$value" =~ ^[0-9]+$ ]] ;;
    RUNNER_LABELS|JOB_LABELS) [[ "$value" =~ ^[A-Za-z0-9._-]+(,[A-Za-z0-9._-]+)*$ ]] ;;
    *) return 1 ;;
  esac
}

load_fixture() {
  local file="$1"
  local line key value sample_number field target_name required_key
  local -A values=()
  [[ -f "$file" && ! -L "$file" ]] || die "fixture must be a regular non-symlink file"
  [[ "$(wc -c < "$file")" -le 65536 ]] || die "fixture is too large"

  while IFS= read -r line || [[ -n "$line" ]]; do
    [[ -z "$line" || "$line" == \#* ]] && continue
    [[ "$line" != *[$'\001'-$'\010'$'\013'$'\014'$'\016'-$'\037'$'\177']* ]] || die "fixture contains control characters"
    [[ "$line" == *=* ]] || die "invalid fixture line"
    key="${line%%=*}"
    value="${line#*=}"
    [[ "$key" != [[:space:]]* && "$key" != *[[:space:]] ]] || die "fixture key has surrounding whitespace"
    [[ -z "$value" || ( "$value" != [[:space:]]* && "$value" != *[[:space:]] ) ]] || die "fixture value has surrounding whitespace"
    [[ "$key" =~ ^SAMPLE([12])_([A-Z_]+)$ ]] || die "unknown fixture key: $key"
    sample_number="${BASH_REMATCH[1]}"
    field="${BASH_REMATCH[2]}"
    [[ " ${fixture_fields[*]} " == *" ${field} "* ]] || die "unknown fixture key: $key"
    [[ -z "${values[$key]+present}" ]] || die "duplicate fixture key: $key"
    validate_fixture_scalar "$field" "$value" || die "invalid fixture value: $key"
    values[$key]="$value"
  done < "$file"

  for sample_number in 1 2; do
    for field in "${fixture_fields[@]}"; do
      required_key="SAMPLE${sample_number}_${field}"
      [[ -n "${values[$required_key]+present}" ]] || die "missing fixture key: $required_key"
    done
  done

  init_sample SAMPLE_ONE
  init_sample SAMPLE_TWO
  for sample_number in 1 2; do
    target_name="SAMPLE_ONE"
    [[ "$sample_number" == 2 ]] && target_name="SAMPLE_TWO"
    local -n target="$target_name"
    target[at]="${values[SAMPLE${sample_number}_AT]}"
    target[api_ok]="${values[SAMPLE${sample_number}_API_OK]}"
    target[ssh_ok]="${values[SAMPLE${sample_number}_SSH_OK]}"
    target[runner_id]="$runner_name"
    target[runner_status]="${values[SAMPLE${sample_number}_RUNNER_STATUS]}"
    target[runner_busy]="${values[SAMPLE${sample_number}_RUNNER_BUSY]}"
    target[runner_labels]="$(normalize_labels "${values[SAMPLE${sample_number}_RUNNER_LABELS]}")"
    target[job_status]="${values[SAMPLE${sample_number}_JOB_STATUS]}"
    target[job_created_at]="${values[SAMPLE${sample_number}_JOB_CREATED_AT]}"
    target[job_started_at]="${values[SAMPLE${sample_number}_JOB_STARTED_AT]}"
    target[job_labels]="$(normalize_labels "${values[SAMPLE${sample_number}_JOB_LABELS]}")"
    target[service_active]="${values[SAMPLE${sample_number}_SERVICE_ACTIVE]}"
    target[listener_count]="${values[SAMPLE${sample_number}_LISTENER_COUNT]}"
    target[worker_count]="${values[SAMPLE${sample_number}_WORKER_COUNT]}"
    finalize_sample "$target_name"
  done
}

sample_valid() {
  local -n sample="$1"
  [[ "${sample[error]}" == "none" ]]
}

sample_busy() {
  local -n sample="$1"
  [[ "${sample[runner_status]}" == "online" && ( "${sample[runner_busy]}" == "true" || "${sample[worker_count]}" -gt 0 ) ]]
}

sample_candidate() {
  local -n sample="$1"
  sample_valid "$1" || return 1
  [[ "${sample[runner_status]}" == "online" &&
     "${sample[runner_busy]}" == "false" &&
     "${sample[job_status]}" == "queued" &&
     "${sample[queue_age_source]}" == "job.created_at" &&
     "${sample[queue_age_seconds]}" -ge "$queue_threshold_seconds" &&
     "${sample[service_active]}" == "true" &&
     "${sample[listener_count]}" -eq 1 &&
     "${sample[worker_count]}" -eq 0 ]] || return 1
  labels_compatible "${sample[runner_labels]}" "${sample[job_labels]}"
}

sample_signature() {
  local -n sample="$1"
  printf '%s|%s|%s|%s|%s|%s|%s|%s|%s' \
    "${sample[runner_id]}" "${sample[runner_status]}" "${sample[runner_busy]}" \
    "$(normalize_labels "${sample[runner_labels]}")" "${sample[job_status]}" \
    "$(normalize_labels "${sample[job_labels]}")" "${sample[service_active]}" \
    "${sample[listener_count]}" "${sample[worker_count]}"
}

print_sample() {
  local ordinal="$1"
  local -n sample="$2"
  local compatible="false"
  if [[ "${sample[runner_labels]}" != "unknown" && "${sample[job_labels]}" != "unknown" ]] \
    && labels_compatible "${sample[runner_labels]}" "${sample[job_labels]}"; then
    compatible="true"
  fi
  printf 'SAMPLE_%s sample_at=%s api_ok=%s ssh_ok=%s runner_id=%s runner_status=%s runner_busy=%s runner_labels=%s job_id=%s job_status=%s job_created_at=%s job_started_at=%s job_started_at_interpretation=%s job_labels=%s queue_age_source=%s queue_age_seconds=%s labels_compatible=%s service_active=%s listener_count=%s worker_count=%s error=%s\n' \
    "$ordinal" "${sample[at]}" "${sample[api_ok]}" "${sample[ssh_ok]}" \
    "${sample[runner_id]}" "${sample[runner_status]}" "${sample[runner_busy]}" \
    "${sample[runner_labels]}" "$job_id" "${sample[job_status]}" \
    "${sample[job_created_at]}" "${sample[job_started_at]:-(empty)}" "${sample[job_started_at_interpretation]}" "${sample[job_labels]}" \
    "${sample[queue_age_source]}" "${sample[queue_age_seconds]}" "$compatible" \
    "${sample[service_active]}" "${sample[listener_count]}" "${sample[worker_count]}" "${sample[error]}"
}

emit_result() {
  local state="$1"
  local persistence="$2"
  local reason="$3"
  local exit_code="$4"
  printf 'STATE=%s\nPERSISTENCE=%s\nREASON=%s\n' "$state" "$persistence" "$reason"
  if [[ "$state" == "SUSPECT_DISPATCH" ]]; then
    print_manual_recovery
  fi
  exit "$exit_code"
}

print_manual_recovery() {
  local manual_only_restart_command="sudo systemctl restart ${service_name}"
  printf 'MANUAL ONLY / NOT EXECUTED: ssh -- %q %q\n' "$ssh_target" "$manual_only_restart_command"
  cat <<EOF
POST_RESTART_CHECKLIST:
  1. After human review, execute the MANUAL ONLY command at most once.
  2. Within 60 seconds verify runner busy=true or job ${job_id} becomes in_progress.
  3. Record repo ${repo}, job ${job_id}, runner ${runner_name}, run/job URL, and journal timestamps.
  4. If pickup does not occur, stop; do not loop restarts or reboot the host.
EOF
}

declare -A SAMPLE_ONE=()
declare -A SAMPLE_TWO=()

if [[ -n "$fixture_path" ]]; then
  load_fixture "$fixture_path"
else
  command -v gh >/dev/null || die "gh is required"
  command -v jq >/dev/null || die "jq is required"
  command -v ssh >/dev/null || die "ssh is required"
  command -v timeout >/dev/null || die "timeout is required"
  gh auth status >/dev/null 2>&1 || {
    init_sample SAMPLE_ONE
    init_sample SAMPLE_TWO
    SAMPLE_ONE[error]="gh_auth_failed"
    SAMPLE_TWO[error]="gh_auth_failed"
    print_sample 1 SAMPLE_ONE
    print_sample 2 SAMPLE_TWO
    emit_result UNKNOWN 0/2 "GitHub authentication is unavailable; no recovery advice." 3
  }
  collect_live_sample SAMPLE_ONE
  printf 'First read-only sample complete; waiting %ss before the one confirmation sample.\n' "$sample_interval_seconds"
  sleep "$sample_interval_seconds"
  collect_live_sample SAMPLE_TWO
fi

print_sample 1 SAMPLE_ONE
print_sample 2 SAMPLE_TWO

if ! sample_valid SAMPLE_ONE || ! sample_valid SAMPLE_TWO; then
  emit_result UNKNOWN 0/2 "A parameter, API, SSH, or timestamp observation is invalid; no recovery advice." 3
fi

if [[ "${SAMPLE_ONE[runner_id]}" != "${SAMPLE_TWO[runner_id]}" ]]; then
  emit_result UNKNOWN 0/2 "Runner identity changed across samples; no recovery advice." 3
fi

if [[ "${SAMPLE_ONE[job_created_at]}" != "${SAMPLE_TWO[job_created_at]}" ]]; then
  emit_result UNKNOWN 0/2 "Job created_at changed across samples; no recovery advice." 3
fi

sample_interval="$((SAMPLE_TWO[at_epoch] - SAMPLE_ONE[at_epoch]))"
if ((sample_interval < 0)); then
  emit_result UNKNOWN 0/2 "Sample timestamps are reversed; no recovery advice." 3
fi

if [[ "${SAMPLE_TWO[runner_status]}" == "offline" ]]; then
  if [[ "${SAMPLE_TWO[runner_busy]}" == "true" || "${SAMPLE_TWO[worker_count]}" -gt 0 ]]; then
    emit_result UNKNOWN 0/2 "Offline API state conflicts with active work evidence; no recovery advice." 3
  fi
  emit_result OFFLINE 0/2 "Runner API is offline with no contradictory active Worker evidence." 2
fi

if sample_busy SAMPLE_ONE || sample_busy SAMPLE_TWO; then
  emit_result HEALTHY_BUSY 0/2 "Runner API busy or Worker activity vetoes restart advice." 0
fi

if [[ "${SAMPLE_ONE[service_active]}" != "true" || "${SAMPLE_TWO[service_active]}" != "true" \
   || "${SAMPLE_ONE[listener_count]}" -ne 1 || "${SAMPLE_TWO[listener_count]}" -ne 1 ]]; then
  emit_result UNKNOWN 0/2 "Service must be active and Listener count must equal one in both samples." 3
fi

candidate_count=0
sample_candidate SAMPLE_ONE && candidate_count=$((candidate_count + 1))
sample_candidate SAMPLE_TWO && candidate_count=$((candidate_count + 1))

if ((candidate_count == 2)) \
  && ((sample_interval >= sample_interval_seconds)) \
  && [[ "$(sample_signature SAMPLE_ONE)" == "$(sample_signature SAMPLE_TWO)" ]]; then
  emit_result SUSPECT_DISPATCH 2/2 "Two matching online-idle samples confirm a compatible queued job aged at least 300s." 1
fi

if ((candidate_count > 0)); then
  emit_result QUEUE_GRACE "${candidate_count}/2" "The candidate did not persist with one signature for at least 60s; no recovery advice." 8
fi

if [[ "${SAMPLE_TWO[job_status]}" == "queued" ]] \
  && labels_compatible "${SAMPLE_TWO[runner_labels]}" "${SAMPLE_TWO[job_labels]}" \
  && [[ "${SAMPLE_TWO[queue_age_seconds]}" -lt "$queue_threshold_seconds" ]]; then
  emit_result QUEUE_GRACE 0/2 "Compatible queue age is below 300s; no recovery advice." 8
fi

emit_result HEALTHY_IDLE 0/2 "No persistent compatible queue stall is present; no recovery advice." 0
