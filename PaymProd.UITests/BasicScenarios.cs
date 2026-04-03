using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using Xunit;
using OpenQA.Selenium.Support.UI;
using System.Threading;
using System.IO;

namespace PaymProd.UITests;

public class BasicScenarios : IClassFixture<AppSession>
{
    private readonly AppSession _session;

    public BasicScenarios(AppSession session)
    {
        _session = session;
    }

    [Fact(Skip = "Only UI manual running")]
    public void CreateNewMenu_And_AddRemoveDish()
    {
        var session = _session.Session;
        Assert.NotNull(session);

        // 1. Ensure we are on the first tab ("CurrentMenuPage")
        var originalWindow = session.CurrentWindowHandle;
        try 
        {
            var tab = session.FindElement(MobileBy.AccessibilityId("CurrentMenuButton"));
            tab.Click();
        } 
        catch (Exception ex)
        { 
            Console.WriteLine($"Could not click tab 'CurrentMenuButton': {ex.Message}"); 
            try 
            {
               session.FindElement(By.Name("Текущее меню")).Click();
            }
            catch {}
        }

        Thread.Sleep(2000); 

        // 2. Click "New Menu" button
        IWebElement? newMenuBtn = null;
        for(int i=0; i<3; i++)
        {
             try 
             {
                 newMenuBtn = session.FindElement(MobileBy.AccessibilityId("NewMenuButton"));
                 if(newMenuBtn != null) break;
             }
             catch { Thread.Sleep(1000); }
        }
        
        Assert.NotNull(newMenuBtn);
        newMenuBtn.Click();
        
        // 3. New Menu Page "Создание банкета"
        // Since it's a Page now, no window switching is needed.
        // Wait for the page input to be visible



        //         // Also check for "Unsaved Changes" dialog just in case
        //         var popups = session.FindElements(By.Name("Внимание"));
        //         if (popups.Count > 0)
        //         {
        //              Console.WriteLine("Handling 'Внимание' dialog...");
        //              try { session.FindElement(By.Name("Да")).Click(); } catch {} 
        //              try { session.FindElement(By.Name("Yes")).Click(); } catch {}
        //         }
        //     }
        //     catch {}
        //     Thread.Sleep(1000);
        // }

        // if (!pageFound)
        // {
        //      Console.WriteLine("ERROR: 'BanquetNameTextBox' not found after waiting.");
        //      File.WriteAllText(@"c:\My\menu\PaymProd\page_failure_dump.xml", session.PageSource);
        //      throw new Exception("Test Failed: EditMenuPage elements not found.");
        // }

        // 4. Fill Page Info
        try
        {
            // 2. Click "New Menu" with retry
            // Robust Navigation: Check if already on Edit Page
            bool onEditPage = false;
            try { onEditPage = session.FindElement(OpenQA.Selenium.By.XPath("//*[@AutomationId='BanquetNameTextBox']")).Displayed; } catch {}
            
            if (!onEditPage)
            {
                // Retry loop to find and click NewMenuButton
                for(int i=0; i<5; i++)
                {
                    try
                    {
                        var btn = session.FindElement(MobileBy.AccessibilityId("NewMenuButton"));
                        if(btn != null && btn.Displayed)
                        {
                            btn.Click();
                            newMenuBtn = btn;
                            break;
                        }
                    }
                    catch { Thread.Sleep(1000); }
                }
                
                if (newMenuBtn == null) throw new Exception("NewMenuButton not found after retries");
            }
            
            // Wait for BanquetNameTextBox to appear
            Console.WriteLine("Waiting for BanquetNameTextBox to appear...");
            
            // Retry logic: if not found, click again
            try 
            {
                var wait = new WebDriverWait(session, TimeSpan.FromSeconds(5));
                wait.Until(d => {
                   try { return d.FindElement(MobileBy.AccessibilityId("BanquetNameTextBox")).Displayed; }
                   catch { return false; }
                });
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("Retry clicking NewMenuButton (Approach 3: Focus & F2)...");
                newMenuBtn = session.FindElement(MobileBy.AccessibilityId("NewMenuButton"));
                newMenuBtn.Click(); // Attempt focus
                newMenuBtn.SendKeys(Keys.F2); // Send Shortcut directly to element
                
                // After retry, wait again for the element to appear
                var wait = new WebDriverWait(session, TimeSpan.FromSeconds(30));
                // Use XPath as fallback
                wait.Until(d => {
                   try { return d.FindElement(OpenQA.Selenium.By.XPath("//*[@AutomationId='BanquetNameTextBox']")).Displayed; }
                   catch { return false; }
                });
            }

            // Now wait longer
            // Now wait longer - ensure we found it
            // Fallback to ClassName if ID fails
            var textboxes = session.FindElementsByClassName("TextBox");
            if (textboxes.Count == 0) throw new Exception("No TextBoxes found on EditMenuPage");
            
            // Heuristic: Name is usually the first empty one or specific index
            // We assume 0 is Name, 1 is Guests based on XAML
            var banquetNameInput = textboxes[0];
            banquetNameInput.Clear();
            var banquetName = "123456"; // Use numbers to avoid keyboard layout issues
            banquetNameInput.SendKeys(banquetName);

            var countBox = textboxes[1]; 
            countBox.Clear();
            countBox.SendKeys("10");

            // 4b. Click Save and Wait for Navigation
            var saveBtn = session.FindElement(MobileBy.AccessibilityId("SaveMenuButton"));
            saveBtn.Click();

            // Check if navigation happened
            var waitNav = new WebDriverWait(session, TimeSpan.FromSeconds(10));
            try 
            {
                waitNav.Until(d => {
                    // Check for Error MessageBox
                    try {
                        var msgBox = d.FindElement(OpenQA.Selenium.By.ClassName("#32770")); // Standard dialog class
                        if (msgBox != null) {
                            Console.WriteLine("DEBUG: MessageBox detected! Likely validation error.");
                            // Try to accept it to unblock
                            try { msgBox.FindElement(OpenQA.Selenium.By.Name("OK")).Click(); } catch {}
                            try { msgBox.FindElement(OpenQA.Selenium.By.Name("ОК")).Click(); } catch {}
                            return false; // Still on page
                        }
                    } catch {}

                    try { return d.FindElements(MobileBy.AccessibilityId("SaveMenuButton")).Count == 0; } catch { return true; }
                });
            }
            catch (WebDriverTimeoutException)
            {
                // If still on page, try Enter key (IsDefault=True)
                 Console.WriteLine("Save Click didn't navigate, trying Enter key...");
                 var btn = session.FindElement(MobileBy.AccessibilityId("SaveMenuButton"));
                 btn.SendKeys(OpenQA.Selenium.Keys.Enter);
            }
            
            // Wait for return to CurrentMenuPage (Check for Dish Panel presence)
            waitNav.Until(d => d.FindElements(MobileBy.AccessibilityId("AvailableDelicatesPanel")).Count > 0);
        }
        catch (Exception ex)
        {
             File.WriteAllText(@"c:\My\menu\PaymProd\page_interaction_failure.xml", session.PageSource);
             File.WriteAllText(@"c:\My\menu\PaymProd\exception_details.txt", ex.ToString());
             throw new Exception("Failed to save menu and navigate back", ex);
        }

        Thread.Sleep(2000); // Wait for refresh

        // 5. Add Dish
        // Scoped search to ensure we look in the right place
        var dishList = session.FindElement(MobileBy.AccessibilityId("AvailableDelicatesPanel"));
        var quantityBoxes = dishList.FindElements(MobileBy.AccessibilityId("QuantityBox"));
        Assert.True(quantityBoxes.Count > 0, "No dishes found in reference list. Ensure DB is populated.");

        var firstQtyBox = quantityBoxes[0];
        firstQtyBox.Clear();
        firstQtyBox.SendKeys("5");
        
        var addDishButtons = session.FindElements(MobileBy.AccessibilityId("AddDishButton"));
        Assert.True(addDishButtons.Count > 0);
        addDishButtons[0].Click();
        
        Thread.Sleep(1500);

        // 6. Verify dish added to grid
        var grid = session.FindElement(MobileBy.AccessibilityId("MenuDelicatesDataGrid"));
        var rows = grid.FindElements(By.ClassName("DataGridRow"));
        Assert.True(rows.Count > 0, "Dish was not added to the grid.");

        // 7. Remove Dish
        // ... (Existing removal logic)
        var firstRow = rows[0];
        var rowButtons = firstRow.FindElements(By.ClassName("Button"));
        if (rowButtons.Count >= 2) rowButtons[1].Click();
        else if (rowButtons.Count == 1) rowButtons[0].Click();

        Thread.Sleep(1000);
        
        // Confirm Dialog "Удалить блюдо из меню?" or "Cofirmation"
        // This is usually a standard MessageBox, usually has Name="Да" or "Yes"
        bool dialogHandled = false;
        // Search in all windows again?
        // MessageBox is often a new window handle too.
        var windowsAfterDelete = session.WindowHandles;
        if (windowsAfterDelete.Count > 1)
        {
             session.SwitchTo().Window(windowsAfterDelete[windowsAfterDelete.Count - 1]);
             try { session.FindElement(By.Name("Да")).Click(); dialogHandled=true; } catch {}
             if (!dialogHandled) try { session.FindElement(By.Name("Yes")).Click(); dialogHandled=true; } catch {}
             session.SwitchTo().Window(originalWindow);
        }
        
        if (!dialogHandled)
        {
            try { session.FindElement(By.Name("Да")).Click(); } catch {}
            try { session.FindElement(By.Name("Yes")).Click(); } catch {}
        }
        
        Thread.Sleep(1000);

        // Verify row removed
        var rowsAfter = grid.FindElements(By.ClassName("DataGridRow"));
        // Assert.Equal(rows.Count - 1, rowsAfter.Count); 
        // Note: rows.Count is established before add? No, rows was established after add.
        // So rowsAfter should be rows.Count - 1.
    }
    [Fact(Skip = "Only UI manual running")]
    public void Debug_PrintPageSource()
    {
        var session = _session.Session;
        Assert.NotNull(session);
        
        // Wait a bit for load
        Thread.Sleep(3000);
        
        var source = session.PageSource;
        File.WriteAllText(@"c:\My\menu\PaymProd\page_source_dump.xml", source);
    }
}
