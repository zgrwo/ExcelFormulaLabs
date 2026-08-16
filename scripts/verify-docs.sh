#!/bin/bash
# ============================================================================
# verify-docs.sh - verify-docs.ps1 的跨平台包装器（单一实现）
#
# 历史教训（2026-08 审查）：verify-docs 曾有 ps1/sh 两套独立实现，
# 行为分叉风险高（grep -P 依赖 GNU grep、报告逻辑不一致等）。
# 现统一以 verify-docs.ps1 为唯一实现；本脚本仅在 POSIX 环境调用 pwsh。
# GitHub ubuntu-latest runner 预装 PowerShell 7（pwsh）。
# ============================================================================
set -euo pipefail
cd "$(dirname "$0")/.."
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-docs.ps1 "$@"
