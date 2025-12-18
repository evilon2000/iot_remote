# BOVE IOT 调试工具

[![许可证: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![版本](https://img.shields.io/badge/Version-v1.05-blue.svg)](https://github.com/yourusername/bove-iot-debug)

## 简介

BOVE IOT 调试工具是一个开源的 IoT 设备调试工具，专为嵌入式开发者和 IoT 爱好者设计。它提供了一个直观的图形用户界面（GUI），支持串口通信、Modbus 协议解析、AT 指令发送与响应处理等核心功能。该工具旨在简化 IoT 设备的调试过程，帮助用户快速测试和验证设备行为，而无需编写复杂的脚本。

项目灵感来源于实际的 IoT 项目需求（如 ESP32、Quectel 等模块的串口交互），并支持字符串/十六进制数据格式切换、自动发送/回复等高级特性。当前版本为 v1.05，支持 Windows 平台。

![主界面截图](screenshots/main_interface.png)  
*(截图：工具主界面，显示端口配置、数据发送/接收区、Modbus/AT 模式切换等功能。)*

## 特性

- **串口通信**：支持自定义端口、波特率、数据位、停止位、校验位和流控（Handshaking）。
- **数据格式支持**：字符串和十六进制（Hex）模式，方便调试文本或二进制数据。
- **Modbus 支持**：内置 Modbus RTU 读/写功能，可添加 CRC 校验；支持 Modbus 扫描和脉冲发送。
- **AT 指令解析**：一键发送 AT 命令，支持响应解析（如 +CME: 等格式），适用于 GSM/GPRS/Bluetooth 模块。
- **自动化功能**：
  - AutoSend：定时发送数据（默认 1000ms 间隔）。
  - AutoReply：自动回复模式。
- **日志与监控**：实时显示接收/发送数据计数，端口刷新按钮确保最新设备列表。
- **用户友好界面**：简洁的 GUI，支持一键清空数据、清空发送历史。

## 系统要求

- **操作系统**：Windows 10/11（当前仅支持 Windows，未来计划跨平台）。
- **依赖**：.NET Framework 4.7.2 或更高版本（使用 Visual Studio 构建）。
- **硬件**：标准 USB-to-Serial 适配器（如 CP210x、FTDI）用于串口连接。

## 安装

### 预编译版本
1. 从 [Releases](https://github.com/yourusername/bove-iot-debug/releases) 下载最新版本的 `.exe` 文件（例如 `BOVE_IOT_v1.05.exe`）。
2. 解压并运行 `BOVE_IOT_v1.05.exe`。
3. 无需额外安装，直接连接串口设备即可使用。

### 从源代码构建
1. 克隆仓库：
   ```
   git clone https://github.com/yourusername/bove-iot-debug.git
   cd bove-iot-debug
   ```
2. 打开项目文件 `BOVE_IOT.sln` 使用 Visual Studio 2022（或更高版本）。
3. 安装 NuGet 包（如果有）：右键解决方案 > 还原 NuGet 包。
4. 构建项目：生成 > 构建解决方案（Release 模式）。
5. 输出文件位于 `bin/Release/` 目录。

**注意**：确保您的系统有串口驱动（如 CH340/PL2303）。如果端口未显示，使用“刷新”按钮更新列表。

## 使用指南

### 基本操作
1. **配置端口**：
   - 在“COM Name”下拉菜单选择串口（如 COM3）。
   - 设置“Port Rate”（波特率，默认 115200）、“Data Bits”（8）、“Stop Bits”（1）、“Parity”（None）。
   - “Handshaking”选择流控模式（None/RTS/CTS 等）。

2. **发送数据**：
   - 在“Send”文本框输入数据（字符串或 Hex）。
   - 选择模式：Modbus（读/写）、Hex、AT 或 Pulse。
   - 点击“Send”按钮发送。
   - 启用“Add CRC”以自动添加 Modbus CRC。

3. **接收数据**：
   - 选择接收格式（String/Hex）。
   - 数据将实时显示在“Received”区域。
   - 使用“Clear”清空显示。

4. **自动化**：
   - 勾选“AutoSend”，设置间隔（ms），点击“Send”启用定时发送。
   - 勾选“AutoReply”启用自动响应模式。

### 示例：测试 AT 指令
- 连接 ESP32 模块（波特率 115200）。
- 输入 `AT` 并选择“AT”模式，点击 Send。
- 预期响应：`OK`（显示在 Received 区域）。

### 示例：Modbus 读寄存器
- 选择“Modbus”模式，输入从站地址 + 功能码 + 寄存器地址（Hex 格式，如 `01 03 00 00 00 01`）。
- 启用“Add CRC”，点击 Send。
- 响应将解析并显示。

**故障排除**：
- “Not Connected”：检查端口是否被占用，重启工具。
- 无响应：验证波特率匹配设备，检查线缆连接。
- Hex 模式下，确保输入为有效十六进制（无空格）。

## 截图

- **主界面**：端口配置与数据交互区。
  ![主界面](screenshots/main_interface.png)
- **Modbus 示例**：发送 Modbus 命令并接收响应。
  *(添加更多截图到 `screenshots/` 目录。)*

## 贡献

欢迎贡献！请遵循以下步骤：
1. Fork 项目。
2. 创建功能分支（`git checkout -b feature/AmazingFeature`）。
3. 提交更改（`git commit -m 'Add some AmazingFeature'`）。
4. Push 到分支（`git push origin feature/AmazingFeature`）。
5. 打开 Pull Request。

**问题报告**：使用 [Issues](https://github.com/yourusername/bove-iot-debug/issues) 提交 bug 或功能请求。包括截图、日志和复现步骤。

## 许可证

本项目采用 MIT 许可证。详见 [LICENSE](LICENSE) 文件。

## 联系与支持

- **作者**： [evilon2000](https://github.com/evilon2000)
- **讨论**：在 GitHub Discussions 中提问，或通过 Issues 反馈。
- **未来计划**：添加 Linux/Mac 支持、多线程日志、脚本自动化和 WebSocket 集成。

感谢您的兴趣！如果您在 IoT 项目中使用此工具，欢迎分享您的故事~ 🚀
