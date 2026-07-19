---
name: Codex 小切片
about: 具有紧凑上下文和明确验收标准的单一实现任务
title: "[切片] "
labels: ""
assignees: ""
---

## 目标 / Goal

-

## 上下文包 / Context pack

目标文件：
-

相关入口：
- AGENTS.md

非目标：
-

## 验收标准

- [ ]

## 必需门禁 / Required gates

- dotnet build ProceduralRts.csproj --no-restore
- git diff --check

## 异步 CI / Async CI

- 窄门通过后创建草稿 PR。
- 不在当前会话等待 VerifyAll；后续使用 `sh tools/ci-monitor.sh <pr-number>` 检查。

## 证据位置 / Evidence destination

- 证据仅存放在 GitHub PR、Issue 评论和 CI artifact。
<!-- GitHub PR, issue comments, and CI artifacts only -->

## 已知风险 / Known risks

-

## 完成条件 / Done

-
