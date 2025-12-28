# TcpLicenseServer

![Status](https://img.shields.io/badge/Status-Work_in_Progress-yellow)
![Version](https://img.shields.io/badge/Version-Alpha-red)

> ⚠️ **DISCLAIMER: Work in Progress**
> 
> This project is currently in early development/alpha stage. It is intended for educational purposes or as a Proof of Concept (PoC).
> **Do not use in a production environment.** Features, database schema, and protocol commands are subject to change without notice.

A lightweight, high-performance TCP server designed for handling software licensing, user authentication, and remote configuration management. Built with **.NET** (C#), it utilizes a command pattern architecture, **SQLite** for data persistence, and **Serilog** for robust logging.

## 🚀 Features

*   **TCP Socket Communication**: Fast, raw socket communication using `TcpListener`.
*   **Asynchronous Architecture**: Fully async/await implementation for handling multiple concurrent clients.
*   **Authentication System**: 
    *   Key-based login.
    *   **HWID Locking**: Binds a license key to a specific hardware ID.
    *   Subscription expiration checks.
    *   Ban system.
*   **Role-Based Access Control**: Decorator-based permission system (`AdminOnlyAttribute`) to separate Admin and User commands.
*   **Remote Configuration**: Users can store and retrieve JSON configurations associated with their license.
*   **Extensible Command Pattern**: Easily add new commands by implementing the `ICommand` interface.
*   **Data Persistence**: Uses **Entity Framework Core** with SQLite.

## 🛠️ Tech Stack

*   **Language**: C# (.NET 10.0 recommended)
*   **ORM**: Entity Framework Core
*   **Database**: SQLite (`licenseServer.db`)
*   **Logging**: Serilog (Console & File)

## 📦 Getting Started

### Prerequisites
*   .NET SDK installed.

### Installation

1.  Clone the repository:
    ```bash
    git clone https://github.com/larkliy/TcpLicenseServer.git
    ```
2.  Navigate to the project directory:
    ```bash
    cd TcpLicenseServer/TcpLicenseServer
    ```
3.  Build the project:
    ```bash
    dotnet build
    ```
4.  Run the server:
    ```bash
    dotnet run
    ```
    *The server will automatically create the `licenseServer.db` database file on the first run.*

## 📡 Protocol & Usage

The server communicates via text-based commands over TCP on port `8080`.
**Format**: `COMMAND arg1 arg2 ...`

### Authentication
To access any functionality, the client must first log in.

*   **Command**: `LOGIN [LicenseKey] [HWID]`
*   **Example**: `LOGIN admin-secret-key MyCpuId123`

### User Commands
Available to all authenticated users.

| Command | Arguments | Description |
| :--- | :--- | :--- |
| `CONFIGCREATE` | `[Name] [JSON content]` | Saves a new configuration string. |
| `GETCONFIG` | `[Name]` | Retrieves a specific configuration. |
| `GETCONFIGS` | None | Lists all configurations owned by the user. |

### Admin Commands
Available only if the user has the `Admin` role.

| Command | Arguments | Description |
| :--- | :--- | :--- |
| `USERCREATE` | `[Key] [Role] [SubEndDate]` | Creates a new user. Default role is "User". |
| `USERBANUNBAN` | `[Key]` | Toggles the ban status of a user. |
| `USERHWIDUPDATE` | `[Key] [NewHWID]` | Resets or updates a user's HWID binding. |
| `USERSUBSCRIPTIONDATEUPDATE` | `[Key] [DateTime]` | Extends or reduces a subscription. |
| `USERINFO` | `[Key]` | Returns full JSON details about a user. |
| `GETALLUSERS` | `[Page] [Size]` | Lists all users (paginated). |
| `GETALLCONFIGS` | `[Page] [Size]` | Lists all configs on the server. |

## 📂 Project Structure

*   **Commands/**: Contains logic for all executable commands.
*   **Decorators/**: Handles middleware logic like `AuthGuard` and `CheckOnAdmin`.
*   **Data/**: EF Core DbContext and database configuration.
*   **Models/**: Database entities (`User`, `Config`) and session models.
*   **MainServer.cs**: Handles the TCP listener and client acceptance loop.
*   **CommandFactory.cs**: Uses Reflection to dynamically register commands.

## 🚧 Current Status & Limitations

This server is currently a **prototype**. The following features are planned but not yet implemented:

*   [ ] **Encryption**: Traffic is currently sent in plain text (No SSL/TLS).
*   [ ] **Unit Tests**: Test coverage is pending.
*   [ ] **Migrations**: EF Core migrations are not yet set up (relies on `EnsureCreated`).

**Pull requests and contributions are welcome!**

## 📝 License

This project is open-source. Feel free to modify and distribute it as needed.
