# 🚀 Migration Tool Quick Reference Card

## 📦 What You Got

A **standalone console application** in the `MigrationTool/` folder that migrates your database from SQL Server LocalDB to SQLite.

---

## ⚡ Quick Commands

### Run Now (with .NET SDK)
```powershell
dotnet run --project MigrationTool
```

### Build Standalone .exe
```powershell
.\build-migration-tool.bat
```
Creates: `PaymProdMigrate.exe` (~70-80 MB, no installation needed)

### Run the Executable
```powershell
.\PaymProdMigrate.exe
```

---

## 📁 Files Created

| File | Purpose |
|------|---------|
| `MigrationTool/Program.cs` | Main application code |
| `MigrationTool/MigrationTool.csproj` | Project configuration |
| `MigrationTool/README.md` | Technical documentation |
| `MigrationTool/QUICK_START.txt` | Quick reference |
| `build-migration-tool.bat` | Build standalone .exe |
| `MIGRATION_TOOL_GUIDE.md` | Complete guide |
| `STANDALONE_MIGRATION_TOOL.md` | Overview & features |

---

## 🎯 Key Features

✅ **Standalone executable** - no .NET SDK required to run  
✅ **Color-coded output** - green ✓, yellow ⚠️, red ✗  
✅ **Detailed statistics** - see exactly what was migrated  
✅ **Command-line args** - custom paths supported  
✅ **Error handling** - helpful error messages  
✅ **Single file** - easy to distribute

---

## 💻 Usage

```powershell
# Basic
PaymProdMigrate.exe

# Custom source
PaymProdMigrate.exe "C:\Data\MenuCaolc.mdf"

# Custom source and target
PaymProdMigrate.exe "C:\Data\MenuCaolc.mdf" "D:\Output\MenuCalc.db"
```

---

## 📊 What It Migrates

From: `MenuCaolc.mdf` (SQL Server LocalDB)  
To: `MenuCalc.db` (SQLite)

- ✅ Products (104)
- ✅ Dishes (32)
- ✅ Recipes (164)
- ✅ Types & Units
- ⚠️ Menus (column mismatch)

---

## 📖 Documentation

| Document | Description |
|----------|-------------|
| **MIGRATION_TOOL_GUIDE.md** | 📘 Complete guide |
| **STANDALONE_MIGRATION_TOOL.md** | 📗 Overview & technical |
| **MigrationTool/README.md** | 📙 Tool-specific docs |
| **MigrationTool/QUICK_START.txt** | 📄 Text reference |

---

## 🔧 Requirements

**To Build:**
- .NET 9.0 SDK
- Windows 10/11 x64

**To Run:**
- Windows 10/11 x64
- SQL Server LocalDB
- **NO .NET SDK** (if using standalone .exe)

---

## 🚨 Troubleshooting

| Problem | Solution |
|---------|----------|
| "File not found" | Place MenuCaolc.mdf in same folder |
| "Cannot connect" | Install SQL Server LocalDB or Express |
| "Build failed" | Install .NET 9.0 SDK |

---

## 📍 Output Location

```
C:\Users\<YourName>\AppData\Local\PaymProdNet9\MenuCalc.db
```

---

## 🎉 Success!

Your migration tool is ready to:
- ✅ Run immediately with .NET SDK
- ✅ Build as standalone .exe
- ✅ Distribute to others
- ✅ Migrate your database
- ✅ Show detailed results

**Just run:** `dotnet run --project MigrationTool` 🚀

---

*v1.0.0 • 2025 • Windows x64*

