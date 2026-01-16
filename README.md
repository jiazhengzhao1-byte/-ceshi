# Focus Blocker

本仓库提供一个 Windows 本地拦截器的实现蓝图与最小可运行实现，目标是“开机即生效”的域名级屏蔽，并提供随开机启动的 UI 用于临时解除。

## 目标摘要

- 默认屏蔽：开机后 1–3 秒内生效，覆盖 B 站/斗鱼/虎牙/知乎/抖音等域名集合。
- 临时解除：UI 提供 5min/10min/1h/2h 按钮，1h/2h 每日仅一次（本地 0 点重置）。
- 立刻生效：当前实现使用 Windows 防火墙 FQDN 规则（适合作为可运行版本），后续可替换为 WFP/包过滤实现。

## 目录结构

```
focus-blocker/
  README.md
  docs/
  config/
  src/
  installer/
  scripts/
```

### 文档

- `docs/prd.md`：需求说明与验收标准。
- `docs/threat-model.md`：权限边界、安全与日志最小化。
- `docs/domain-list-guideline.md`：域名维护规范。
- `docs/qa-test-cases.md`：测试用例清单。

### 配置

- `config/domains.default.json`：默认域名集合（可扩展）。
- `config/appsettings.default.json`：策略与日志配置。

## 构建与安装（Windows）

> 需要 .NET 8 SDK。

1. 构建 Service 与 UI：
   - `dotnet build src/FocusBlocker.Service/FocusBlocker.Service.csproj -c Release`
   - `dotnet build src/FocusBlocker.UI/FocusBlocker.UI.csproj -c Release`
2. 管理员执行安装脚本：
   - `installer/windows/install.ps1`
3. 需要卸载时：
   - `installer/windows/uninstall.ps1`

## 后续落地建议

- 将 `FirewallBlockEngine` 替换为 WFP/包过滤实现，保证解除/恢复的秒级生效。
- 增加连接断开器以在恢复屏蔽时主动中断已建立连接。

