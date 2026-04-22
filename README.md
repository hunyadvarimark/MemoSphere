# 🧠 MemoSphere

> **AI-powered study companion** – organize notes, generate quizzes, and track your learning journey.

MemoSphere is a sophisticated educational application built with **.NET 9 and WPF**, following the **MVVM architectural pattern**. It empowers users to organize study materials, create interactive quizzes, and utilize **Google's Gemini AI** to automatically generate learning content from their notes.

---

## ✨ Key Features

- **Hierarchical Learning** – Organize knowledge into Subjects, Topics, and detailed Notes.
- **AI-Driven Quizzes** – Automatically generate multiple-choice and true/false questions using the Gemini AI API.
- **Smart Dashboard** – Track daily progress and stay focused with an "Active Topics" gamification system.
- **Cloud Persistence** – Seamless data management using Supabase (PostgreSQL) with Entity Framework Core.
- **Modern WPF UI** – Clean, responsive interface with customized styles and asynchronous command handling.

---

## 🛠 Tech Stack

| Layer | Technology |
|---|---|
| Framework | .NET 9 (WPF) |
| Architecture | MVVM (Model-View-ViewModel) |
| Database | PostgreSQL via Supabase |
| ORM | Entity Framework Core (Code First) |
| AI Integration | Google Gemini API |
| Design Patterns | Repository Pattern, Unit of Work, Dependency Injection |

---

## 📋 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- PowerShell (for environment setup)
- A [Supabase](https://supabase.com/) project with a PostgreSQL database
- A [Google Gemini API](https://aistudio.google.com/) key

---

## 🔧 Installation & Setup

API keys and connection strings are managed via environment variables loaded from a `.env` file to keep credentials out of source control.

### 1. Clone the repository

```bash
git clone https://github.com/hunyadvarimark/memosphere.git
cd memosphere
```

### 2. Configure environment variables

Create a `.env` file in the project root and fill in your credentials:

```env
SUPABASE_URL=your_supabase_url
SUPABASE_ANON_KEY=your_supabase_anon_key
SUPABASE_CONNECTION_STRING=your_connection_string
GEMINI_API_KEY=your_gemini_api_key
```

### 3. Run the setup script

Open PowerShell and run the following commands to load the variables for the current session:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\setup-env-from-file.ps1
```

### 4. Initialize the database

Apply EF Core migrations to your Supabase instance via the Package Manager Console:

```powershell
Update-Database
```

### 5. Build and run

Open `MemoSphere.sln` in Visual Studio 2022 and press **F5**.

---

## 🏗 Project Structure

```
MemoSphere/
├── MemoSphere.Core/        # Domain models, Enums, Service interfaces, Repository abstractions
├── MemoSphere.Data/        # EF Core DbContext, Migrations, Repository implementations,
│                           # external service logic (Gemini, Supabase)
└── MemoSphere/             # WPF Views, ViewModels, Converters, UI-specific utilities
```


