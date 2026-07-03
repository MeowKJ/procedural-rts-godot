#!/usr/bin/env sh
set -eu

usage() {
  cat <<'EOF'
Usage: sh tools/ci-monitor.sh [--repo OWNER/REPO] [--strict] [PR...]

Poll PR check status once. It does not wait.

Default behavior:
- If PR numbers are provided, poll those PRs.
- If no PR is provided, list and poll all open PRs.
- Exit 0 even when checks are pending or failing, so Codex can hand off cleanly.

Use --strict to exit nonzero for pending/failing checks.
EOF
}

repo="${GH_REPO:-}"
strict=0
prs=""

while [ "$#" -gt 0 ]; do
  case "$1" in
    --repo)
      shift
      repo="${1:-}"
      ;;
    --strict)
      strict=1
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      prs="$prs $1"
      ;;
  esac
  shift || true
done

if [ -z "$repo" ]; then
  repo="$(gh repo view --json nameWithOwner --jq .nameWithOwner 2>/dev/null || true)"
fi

if [ -z "$repo" ]; then
  echo "Could not infer GitHub repo. Pass --repo OWNER/REPO." >&2
  exit 2
fi

if [ -z "$prs" ]; then
  prs="$(gh pr list --repo "$repo" --state open --json number --jq '.[].number' | tr '\n' ' ')"
fi

if [ -z "$prs" ]; then
  echo "No open PRs in $repo."
  exit 0
fi

overall=0

for pr in $prs; do
  number="$(gh pr view "$pr" --repo "$repo" --json number --jq .number)"
  title="$(gh pr view "$pr" --repo "$repo" --json title --jq .title)"
  url="$(gh pr view "$pr" --repo "$repo" --json url --jq .url)"
  head="$(gh pr view "$pr" --repo "$repo" --json headRefName --jq .headRefName)"

  echo "PR #$number $title"
  echo "Branch: $head"
  echo "URL: $url"

  set +e
  summary="$(gh pr checks "$pr" --repo "$repo" --json bucket --jq 'group_by(.bucket) | map("\(.[0].bucket)=\(length)") | join(" ")' 2>/dev/null)"
  check_rc=$?
  set -e

  if [ -z "$summary" ]; then
    summary="no-checks"
  fi

  if [ "$summary" = "no-checks" ]; then
    state="status:ci-pending"
    overall=8
  elif echo "$summary" | grep -Eq '(^| )pending='; then
    state="status:ci-pending"
    overall=8
  elif echo "$summary" | grep -Eq '(^| )(fail|cancel)='; then
    state="status:needs-fix"
    overall=1
  elif [ "$check_rc" -eq 0 ]; then
    state="status:verified"
  else
    state="status:needs-fix"
    overall=1
  fi

  echo "State: $state ($summary)"
  set +e
  gh pr checks "$pr" --repo "$repo" --json name,bucket,link --template '{{range .}}{{printf "  %-8s %s" .bucket .name}}{{if .link}}{{printf " %s" .link}}{{end}}{{printf "\n"}}{{end}}'
  set -e
  echo
done

if [ "$strict" -eq 1 ] && [ "$overall" -ne 0 ]; then
  exit "$overall"
fi

exit 0
