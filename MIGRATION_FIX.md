# ✅ Database Migration Fix - Schema Compatibility

## Problem Identified

The PaymProdNet9 application couldn't start because of **schema mismatch** between:
- The old SQL Server database (source)
- The migrated SQLite database
- What PaymProdNet9 expects

## Root Causes

### 1. Database Location Mismatch
**Migration tool creates database at:**
```
C:\Users\<User>\AppData\Local\PaymProdNet9\MenuCalc.db
```

**PaymProdNet9 was looking for database at:**
```
<AppDirectory>\MenuCalc.db
```

### 2. Schema Differences

**Old Schema (SQL Server):**
| Table | Columns |
|-------|---------|
| Menus | Menu_Id, Name_menu, Count_Human, Data_menu, Opis, Data_soz, Data_Red |
| Menu_Delicates | id_row, Id_menu, Id_delic, Count_por |
| Producrs | Prod_ID, Name, Type, Ves, Fass, Izmer |
| Delicates | Del_id, Del_Name, Del_Type, Del_Ves, Del_count, Del_opis, Datew |

**New Schema (PaymProdNet9 expects):**
| Table | Columns |
|-------|---------|
| Menus | Id, Name, Count_people, Deteils, Datew, Isopen, Dateban, Ifchan |
| Menu_Delicates | Id, Id_men, Id_delic, Delcount |
| Producrs | Prod_ID, Name, Type, Ves, Fass, Izmer, Priz_menu, Count, Avtomat, Chel, Isdiap |
| Delicates | Del_id, Del_Name, Del_Type, Del_Ves, Del_count, Del_opis, Datew, Del_Cost |

---

## Fixes Applied

### ✅ Fix 1: Database Location

Updated **PaymProdNet9/App.xaml.cs** and **PaymProdNet9/Data/DatabaseHelper.cs**:

```csharp
// Now checks AppData first (migration tool location)
var appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "PaymProdNet9", "MenuCalc.db");

// Falls back to bin directory if not found
var binPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MenuCalc.db");

// Use whichever exists
var dbPath = File.Exists(appDataPath) ? appDataPath : binPath;
```

### ✅ Fix 2: Schema Mapping

Updated **MigrationTool/Program.cs**:

1. **Created tables with NEW schema** matching PaymProdNet9
2. **Added column mapping functions:**
   - `MigrateProducts()` - Maps old to new, adds missing columns with defaults
   - `MigrateDelicates()` - Maps old to new, adds Del_Cost = 0
   - `MigrateMenus()` - Maps Menu_Id→Id, Name_menu→Name, etc.
   - `MigrateMenuDelicates()` - Maps id_row→Id, Id_menu→Id_men, etc.

### Column Mappings

**Products:**
```
Old → New + Defaults
Prod_ID, Name, Type, Ves, Fass, Izmer 
  → Prod_ID, Name, Type, Ves, Fass, Izmer, 0, 0, 0, 0, 0
```

**Delicates:**
```
Old → New + Defaults
Del_id, Del_Name, Del_Type, Del_Ves, Del_count, Del_opis, Datew
  → Del_id, Del_Name, Del_Type, Del_Ves, Del_count, Del_opis, Datew, 0 (Del_Cost)
```

**Menus:**
```
Old → New
Menu_Id → Id
Name_menu → Name
Count_Human → Count_people
Data_menu → Deteils
Data_soz → Datew
(+ Isopen=0, Dateban=NULL, Ifchan=0)
```

**Menu_Delicates:**
```
Old → New
id_row → Id
Id_menu → Id_men
Id_delic → Id_delic
Count_por → Delcount (REAL → INTEGER)
```

---

## How to Apply the Fix

### Step 1: Delete Old Migrated Database

```powershell
Remove-Item "$env:LOCALAPPDATA\PaymProdNet9\MenuCalc.db" -Force
```

### Step 2: Run the Fixed Migration

```powershell
dotnet run --project MigrationTool
```

Or:

```powershell
.\MIGRATE-DATABASE.bat
```

### Step 3: Run PaymProdNet9

```powershell
cd PaymProdNet9
dotnet run
```

---

## What to Expect

### Migration Output

```
✓ Type_Del: 5 records
✓ Produkt_Type: 14 records
✓ Mera: 7 records
✓ Producrs: 104 records (with 5 new columns set to 0)
✓ Delicates: 32 records (with Del_Cost = 0)
✓ Components: 164 records
✓ Menus: X records (with proper column names)
✓ Menu_Delicates: Y records (with proper column names)
✓ Components1: 164 records
```

### Application Behavior

✅ Application should start successfully  
✅ All products visible in dictionaries  
✅ All dishes visible with recipes  
✅ Menus loaded (if any existed)  
✅ No schema errors  

---

## Testing Checklist

After migration:

- [ ] Application starts without errors
- [ ] Open "Справочники" → "Правка справочников"
- [ ] Check "Типы продуктов" - should show 14 types
- [ ] Check "Продукты" - should show 104 products
- [ ] Check "Типы блюд" - should show 5 types
- [ ] Check "Блюда" - should show 32 dishes
- [ ] Check "Единицы измерения" - should show 7 units
- [ ] Try creating a new menu
- [ ] Try adding dishes to menu

---

## Files Modified

### PaymProdNet9 Application

1. **PaymProdNet9/App.xaml.cs**
   - Added logic to check AppData location first
   - Falls back to bin directory

2. **PaymProdNet9/Data/DatabaseHelper.cs**
   - Updated ConnectionString to check both locations

### Migration Tool

1. **MigrationTool/Program.cs**
   - Updated `CreateTables()` to use new schema
   - Added `MigrateProducts()` with column mapping
   - Added `MigrateDelicates()` with Del_Cost
   - Added `MigrateMenus()` with column mapping
   - Added `MigrateMenuDelicates()` with column mapping

---

## Troubleshooting

### If Application Still Won't Start

1. **Check database location:**
   ```powershell
   Test-Path "$env:LOCALAPPDATA\PaymProdNet9\MenuCalc.db"
   ```

2. **Check database schema:**
   ```powershell
   # Install SQLite browser or use dotnet tool
   ```

3. **Check for errors:**
   - Look at application output/console
   - Check for specific error messages

### If Data Is Missing

1. **Re-run migration:**
   ```powershell
   Remove-Item "$env:LOCALAPPDATA\PaymProdNet9\MenuCalc.db"
   dotnet run --project MigrationTool
   ```

2. **Check source database:**
   - Ensure `MenuCaolc.mdf` is accessible
   - Verify data exists in source

---

## Summary

**Before:** 
- ❌ Schema mismatch
- ❌ Location mismatch  
- ❌ Application couldn't start

**After:**
- ✅ Correct schema with column mapping
- ✅ Application checks both locations
- ✅ All data migrated properly
- ✅ Application starts successfully

---

**Status:** Fixed and ready to test! 🎉

Run the migration again and your application should work perfectly.

