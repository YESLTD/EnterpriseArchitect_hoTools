﻿using System;
using System.Windows.Forms;
using System.Deployment.Application;
using System.Diagnostics;
using System.Collections.Generic;
using hoTools.Utils.Favorites;
using hoTools.ActiveX;
using hoTools.Settings;

using hoTools.EaServices;
using hoTools.Utils;
using hoTools.Utils.Appls;
using hoTools.Utils.ActionPins;
using System.Reflection;

using hoTools.Find;

using GlobalHotkeys;

#region description
//---------------------------------------------------------------
//hoTools
//---------------------------------------------------------------
//Tools developed by Helmut Ortmann
//---------------------------------------------------------------
//Configuration:
//hoTools:    ..\Setp\ActiveXdll.config.xml 
//VAR1:        ..\SetpVAR1\ActiveXdll.config.xml 
//
//Customer=hoTools: "14F09211-3460-47A6-B837-A477491F0A67"
//         ZF_LS:    "F52AB09A-8ED0-4159-9AB4-FFD986983280"
//
//To initialize configuration:
//1. Delete: 
//hoTools:    %APPDATA%ho\hoTools\user.config
//VAR1:        %APPDATA%ho\VAR1\user.config
//2. (De)Install hoTools/hoTools_VAR1
//---------------------------------------------------------------
//Debug:
//1. Build in release mode        (only once to establish correct configuration)
//2. (De)Install Addin            (only once to establish correct configuration)
//3. Rebuild in debug mode
//4. Copy the correct ActiveXdll.config.xml into the ..\AddinClass\src\.. !!
//5. Possibly delete old config file (%APPDATA%ho\hoTools or %APPDATA%ho\hoTools_VAR1)
//---------------------------------------------------------------
#endregion

namespace hoTools
{
    public class AddinClass : EAAddinFramework.EAAddinBase
    {
        string release = "X.X.XXX.XX"; // Major, Minor, Build, free,
                                       // overwritten by product version
        // static due to global key definitions
        static EA.Repository Repository = null;
        static AddinSettings AddinSettings = null;
        static AddinControlGUI AddinControlGUI = null;
        static FindAndReplaceGUI FindAndReplaceGUI = null;

        // ActiveX Controls
        AddinControlGUI m_MyControlGUI = null;
        FindAndReplaceGUI m_FindAndReplace = null;
        AddinSettings m_AddinSettings = null;

        EA.Repository m_repository = null;
        // define menu constants
        const string menuName = "-hoTools";

        const string menuShowWindow = "Show Window";
        const string menuChangeXmlFile = "Change *.xml file for a version controlled package";

        const string menuDisplayBehavior = "Display Behavior";
        const string menuDisplayMethodDefinition = "Locate Operation";
        const string menuLocateType = "Locate Type";
        const string menuLocateCompositeElementorDiagram = "Locate CompositeElementOfDiagram";
        const string menuLocateCompositeDiagramOfElement = "Locate CompositeDiagramOfElement";
        const string menuShowSpecification = "Show Specification";
        const string menuUnlockDiagram = "UnlockDiagram";

        const string menuDeviderLineStyleDia = "---------------Line style Diagram-----------------";
        const string menuLineStyleDiaLH = "Line Style Diagram (Object): Lateral Horizontal";
        const string menuLineStyleDiaLV = "Line Style Diagram (Object): Lateral Vertical";
        const string menuLineStyleDiaTH = "Line Style Diagram (Object): Tree Horizontal";
        const string menuLineStyleDiaTV = "Line Style Diagram (Object): Tree Vertical";
        const string menuLineStyleDiaOS = "Line Style Diagram (Object): Orthogonal Square";

        const string menuDeviderActivity = "-------------Create Activity for Operation ---------------------------";
        const string menuCreateActivityForOperation = "Create Activity for Operation (select operation or class)";
        const string menuUpdateOperationParameter = "Update Operation Parameter for Activity (select Package(recursive), Activity, Class, Interface or Operation)";
        const string menuUpdateActionPin = "Update Action Pin for Package (recursive)";

        const string menuDeviderInteraction = "-------------Create Interaction for Operation ---------------------------";
        const string menuCreateInteractionForOperation = "&Create Interaction for Operation (select operation or class)  ";

        const string menuDeviderStateMachine = "-------------Create Statemachine for Operation ---------------------------";
        const string menuCreateStateMachineForOperation = "&Create Statemachine for Operation (select operation)  ";


        const string menuCorrectTypes = "-------------Correct Type ---------------------------";
        const string menuCorrectType = "Correct types of Attribute, Function (selected Attribute, Function, Class or Package)";

        const string menuDeviderCopyPast = "-------------Move links---------------------------"; 
        const string menuCopyGUIDToClipboard = "Copy GUID / Select Statement to Clipboard";
        const string menuCopyLinksToClipboard = "Copy Links to Clipboard";
        const string menuPasteLinksFromClipboard = "Paste Links from Clipboard";

        const string menuDeviderNote = "-------------Note      ---------------------------"; 
        const string menuAddLinkedNote = "Add linked Note";
        const string menuAddLinkedDiagramNote = "Add linked Diagram Note";

        const string menuUsage = "Find Usage";
        const string menuAbout = "About + Help";

        const string menuDevider = "-----------------------------------------------"; 


        public enum displayMode
        {
            Behavior,
            Method
        }


        /// <summary>
        /// constructor where we set the menuheader and menuOptions
        /// </summary>
        public AddinClass()
            : base()
        {
            try
            {
                this.m_AddinSettings = new AddinSettings();
                AddinSettings = this.m_AddinSettings; // static
            }
            catch (Exception e)
            {
                MessageBox.Show("Error setup 'hoTools' Addin. Error:\n" + e.ToString(), "hoTools Installation error");
            }
            this.menuHeader = "-" + m_AddinSettings.productName;
            this.menuOptions = new string[] { 
                //-------------------------- General  --------------------------//
                //    menuLocateCompositeElementorDiagram,
                //-------------------------- LineStyle --------------------------//
                                        
                //-------------------------- Activity --------------------------//
                menuDeviderActivity,
                menuCreateActivityForOperation, menuUpdateOperationParameter, 
                //menuUpdateActionPin,
                //-------------------------- Interaction --------------------------//
                menuDeviderInteraction,
                menuCreateInteractionForOperation,
                //-------------------------- Interaction --------------------------//
                menuDeviderStateMachine,
                menuCreateStateMachineForOperation,
                //-------------------------- Correct Types ------------------------//
                //menuCorrectTypes, menuCorrectType, 
                //-------------------------- Add Note -----------------------------//
                menuDeviderNote,
                menuAddLinkedNote,menuAddLinkedDiagramNote,

                //-------------------------- Move links ---------------------------//
                menuDeviderCopyPast,
                menuChangeXmlFile,menuCopyLinksToClipboard, menuPasteLinksFromClipboard, 

                menuShowWindow,    
                //---------------------------- About -------------------------------//
                menuDevider, menuAbout };
        }
        /// <summary>
        /// EA_Connect events enable Add-Ins to identify their type and to respond to Enterprise Architect start up.
        /// This event occurs when Enterprise Architect first loads your Add-In. Enterprise Architect itself is loading at this time so that while a Repository object is supplied, there is limited information that you can extract from it.
        /// The chief uses for EA_Connect are in initializing global Add-In data and for identifying the Add-In as an MDG Add-In.
        /// Also look at EA_Disconnect.
        /// </summary>
        /// <param name="rep">An EA.Repository object representing the currently open Enterprise Architect model.
        /// Poll its members to retrieve model data and user interface status information.</param>
        /// <returns>String identifying a specialized type of Add-In: 
        /// - "MDG" : MDG Add-Ins receive MDG Events and extra menu options.
        /// - "" : None-specialized Add-In.</returns>
        public override string EA_Connect(EA.Repository rep)
        {
            HotkeyHandlers.SetupGlobalHotkeys();
            m_repository = rep;
            //int v = rep.LibraryVersion;
            if (rep.IsSecurityEnabled)
            {
                //logInUser = Repository.GetCurrentLoginUser(false);
                //if ((logInUser.Contains("deexortm")) ||
                //     (logInUser.Contains("admin")) ||
                //     (logInUser.Equals(""))
                //    ) logInUserRights = UserRights.ADMIN;
            }
            Favorite.installSearches(m_repository); // install searches
            return "a string";
        }
        public override void EA_OnOutputItemDoubleClicked(EA.Repository Repository, string TabName, string LineText, long ID)
        {

        }

        internal static class HotkeyHandlers
        {
            public static void SetupGlobalHotkeys()
            {
                // lest og global hotkeys
                List<Hotkey> hotkeys = new List<Hotkey>();

                Dictionary<string, Keys> keys = GlobalKeysConfig.getKeys();
                Dictionary<string, Modifiers> modifiers = GlobalKeysConfig.getModifiers();
                Keys key;
                Modifiers modifier1;
                Modifiers modifier2;
                Modifiers modifier3;
                Modifiers modifier4;

                for (int i = 0; i < AddinSettings.globalShortcutsService.Count; i = i + 1)
                {
                    GlobalKeysConfig.GlobalKeysSearchConfig search = AddinSettings.globalShortcutsSearch[i];
                    if (search.Key != "None" & search.SearchName != "")
                    {
                        keys.TryGetValue(search.Key, out key);
                        modifiers.TryGetValue(search.Modifier1, out modifier1);
                        modifiers.TryGetValue(search.Modifier2, out modifier2);
                        modifiers.TryGetValue(search.Modifier3, out modifier3);
                        modifiers.TryGetValue(search.Modifier4, out modifier4);
                        switch (i)
                        {
                            case 0:
                                hotkeys.Add(new Hotkey(key, modifier1 | modifier2 | modifier3 | modifier4, HandleGlobalKeySearch0));
                                break;
                            case 1:
                                hotkeys.Add(new Hotkey(key, modifier1 | modifier2 | modifier3 | modifier4, HandleGlobalKeySearch1));
                                break;
                            case 2:
                                hotkeys.Add(new Hotkey(key, modifier1 | modifier2 | modifier3 | modifier4, HandleGlobalKeySearch2));
                                break;
                            case 3:
                                hotkeys.Add(new Hotkey(key, modifier1 | modifier2 | modifier3 | modifier4, HandleGlobalKeySearch3));
                                break;
                            case 4:
                                hotkeys.Add(new Hotkey(key, modifier1 | modifier2 | modifier3 | modifier4, HandleGlobalKeySearch4));
                                break;
                        }
                    }

                }
                for (int i = 0; i < AddinSettings.globalShortcutsService.Count; i = i + 1)
                {
                    GlobalKeysConfig.GlobalKeysServiceConfig service = AddinSettings.globalShortcutsService[i];
                    if (service.Key != "None" & service.GUID != "")
                    {
                        keys.TryGetValue(service.Key, out key);
                        modifiers.TryGetValue(service.Modifier1, out modifier1);
                        modifiers.TryGetValue(service.Modifier2, out modifier2);
                        modifiers.TryGetValue(service.Modifier3, out modifier3);
                        modifiers.TryGetValue(service.Modifier4, out modifier4);
                        switch (i)
                        {
                            case 0:
                                hotkeys.Add(new Hotkey(key, modifier1 | modifier2 | modifier3 | modifier4, HandleGlobalKeyService0));
                                break;
                            case 1:
                                hotkeys.Add(new Hotkey(key, modifier1 | modifier2 | modifier3 | modifier4, HandleGlobalKeyService1));
                                break;
                            case 2:
                                hotkeys.Add(new Hotkey(key, modifier1 | modifier2 | modifier3 | modifier4, HandleGlobalKeyService2));
                                break;
                            case 3:
                                hotkeys.Add(new Hotkey(key, modifier1 | modifier2 | modifier3 | modifier4, HandleGlobalKeyService3));
                                break;
                            case 4:
                                hotkeys.Add(new Hotkey(key, modifier1 | modifier2 | modifier3 | modifier4, HandleGlobalKeyService4));
                                break;
                        }
                    }

                }
                Form hotkeyForm = new InvisibleHotKeyForm(hotkeys);
                hotkeyForm.Show();
            }
            public static void runGlobalKeySearch(int pos)
            {
                
                    GlobalKeysConfig.GlobalKeysSearchConfig sh = AddinSettings.globalShortcutsSearch[pos];
                    if (sh.SearchName == "") return;
                    try
                    {
                        Repository.RunModelSearch(sh.SearchName, sh.SearchTerm, "", "");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString(), "Error start search '" + sh.SearchName +
                           " " + sh.SearchTerm + "'");
                    }
            }
            public static void runGlobalKeyService(int pos)
            {
                GlobalKeysConfig.GlobalKeysServiceConfig sh = AddinSettings.globalShortcutsService[pos];
                    if (sh.Method == null) return;
                    sh.Invoke(Repository, AddinControlGUI.getText());
            }

            private static void HandleGlobalKeySearch0()
            {
                runGlobalKeySearch(0); 
            }
            private static void HandleGlobalKeySearch1()
            {
                runGlobalKeySearch(1);
            }
            private static void HandleGlobalKeySearch2()
            {
                runGlobalKeySearch(2);
            }
            private static void HandleGlobalKeySearch3()
            {
                runGlobalKeySearch(3);
            }
            private static void HandleGlobalKeySearch4()
            {
                runGlobalKeySearch(4);
            }
            private static void HandleGlobalKeyService0()
            {
                runGlobalKeyService(0);
            }
            private static void HandleGlobalKeyService1()
            {
                runGlobalKeyService(1);
            }
            private static void HandleGlobalKeyService2()
            {
                runGlobalKeyService(2);
            }
            private static void HandleGlobalKeyService3()
            {
                runGlobalKeyService(3);
            }
            private static void HandleGlobalKeyService4()
            {
                runGlobalKeyService(4);
            }
                      
        }
        public override void EA_OnPostInitialized(EA.Repository rep)
        {
            // gets the file 'AssemblyVersion' of file
            string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
            release = productVersion;
            Repository = rep;
            m_repository = rep;
            ShowAddinControlWindows();
        }
        public override bool EA_OnPostNewConnector(EA.Repository rep, EA.EventProperties eventProperties)
        {
            EA.EventProperty eventProperty = eventProperties.Get(0);
            string s = (string)eventProperty.Value;
            int connectorID = int.Parse(s);
            EA.Diagram dia = rep.GetCurrentDiagram();
            switch (dia.Type)
            {
                case "Activity":
                    return updateLineStyle(rep, dia, connectorID, m_AddinSettings.ActivityLineStyle.Substring(0, 2));


                case "Statechart":
                    return updateLineStyle(rep, dia, connectorID, m_AddinSettings.StatechartLineStyle.Substring(0, 2));

                case "Logical":
                    return updateLineStyle(rep, dia, connectorID, m_AddinSettings.ClassLineStyle.Substring(0, 2));


                case "Custom":
                    return updateLineStyle(rep, dia, connectorID, m_AddinSettings.CustomLineStyle.Substring(0, 2));

                case "Component":
                    return updateLineStyle(rep, dia, connectorID, m_AddinSettings.ComponentLineStyle.Substring(0, 2));

                case "Deployment":
                    return updateLineStyle(rep, dia, connectorID, m_AddinSettings.DeploymentLineStyle.Substring(0, 2));

                case "Package":
                    return updateLineStyle(rep, dia, connectorID, m_AddinSettings.PackageLineStyle.Substring(0, 2));

                case "Use Case":
                    return updateLineStyle(rep, dia, connectorID, m_AddinSettings.UseCaseLineStyle.Substring(0, 2));

                case "CompositeStructure":
                    return updateLineStyle(rep, dia, connectorID, m_AddinSettings.CompositeStructureLineStyle.Substring(0, 2));

                default:
                    return false;



            }
        }

        public override void EA_FileOpen(EA.Repository rep)
        {
            initializeForRepository(rep);
        }
        public override void EA_FileClose(EA.Repository rep)
        {
            if (m_MyControlGUI != null)  m_MyControlGUI.Save(); // save settings
            m_repository = null;
            rep = null;


        }
        #region initializeForRepository
        private void initializeForRepository(EA.Repository rep)
        {
            m_repository = rep;
            Repository = rep;
            if (m_MyControlGUI != null) m_MyControlGUI.repository = rep;
            if (m_FindAndReplace != null) m_MyControlGUI.repository = rep;


        }
        #endregion

        private bool updateLineStyle(EA.Repository rep, EA.Diagram dia, int connectorID, string style)
        {
            if (style.ToUpper() == "NO") return false;
            foreach (EA.DiagramLink link in dia.DiagramLinks)
            {
                if (link.ConnectorID == connectorID)
                {
                    EaService.setLineStyle(rep, style);
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Called once Menu has been opened to see what menu items should active.
        /// </summary>
        /// <param name="rep">the repository</param>
        /// <param name="Location">the location of the menu</param>
        /// <param name="MenuName">the name of the menu</param>
        /// <param name="ItemName">the name of the menu item</param>
        /// <param name="IsEnabled">boolean indicating whethe the menu item is enabled</param>
        /// <param name="IsChecked">boolean indicating whether the menu is checked</param>
        public override void EA_GetMenuState(EA.Repository rep, string Location, string MenuName, string ItemName, ref bool IsEnabled, ref bool IsChecked)
        {
            if (IsProjectOpen(rep))
            {
                switch (ItemName)
                {
                    case menuChangeXmlFile:
                        IsChecked = false;
                        break;

                    case menuShowWindow:
                        IsChecked = false;
                        break;
                    case  menuShowSpecification:
                        IsChecked = false;
                        break;

                    case menuUnlockDiagram:
                        IsChecked = false;
                        break;

                    case menuLineStyleDiaTH:
                        IsChecked = false;
                        break;
                    case menuLineStyleDiaTV:
                        IsChecked = false;
                        break;
                    case menuLineStyleDiaLH:
                        IsChecked = false;
                        break;
                    case menuLineStyleDiaLV:
                        IsChecked = false;
                        break;
                    case menuLineStyleDiaOS:
                        IsChecked = false;
                        break;

                    case menuLocateCompositeElementorDiagram:
                        IsChecked = false;
                        break;
                    
                    case menuLocateCompositeDiagramOfElement:
                        IsChecked = false;
                        break;
                        

                case menuUsage:
                        IsChecked = false;
                        break;    
                       
                case menuCreateInteractionForOperation:
                        IsChecked = false;
                        break;

                case menuCreateStateMachineForOperation:
                        IsChecked = false;
                        break;  
                    
                case menuCorrectType:
                        IsChecked = false;
                        break;

               case menuDisplayBehavior:
                        IsChecked = false;
                        break;


               case menuUpdateActionPin:
                        IsChecked = false;
                        break;

                    case menuUpdateOperationParameter:
                        IsChecked = false;
                        break;

                    case menuCreateActivityForOperation:
                        IsChecked = false;
                        break;

                    case menuDisplayMethodDefinition:
                        IsChecked = false;
                        break;

                    
                    case menuLocateType:
                        IsChecked = false;
                        break;


                    case menuCopyGUIDToClipboard:
                        IsChecked = false;
                        break;

                    case menuCopyLinksToClipboard:
                        IsChecked = false;
                        break;

                    case menuPasteLinksFromClipboard:
                        IsChecked = false;
                        break;

                    
                    case menuAddLinkedNote:
                        IsChecked = false;
                        break;

                    case menuAddLinkedDiagramNote:
                        IsChecked = false;
                        break;

                    case menuAbout:
                        IsChecked = false;
                        break;

                    // there shouldn't be any other, but just in case disable it.
                    default:
                        IsEnabled = false;
                        break;
                }
            }
            else
            {
                // If no open project, disable all menu options
                IsEnabled = false;
            }
        }

        /// <summary>
        /// Called when user makes a selection in the menu.
        /// This is your main exit point to the rest of your Add-in
        /// </summary>
        /// <param name="rep">the repository</param>
        /// <param name="Location">the location of the menu</param>
        /// <param name="MenuName">the name of the menu</param>
        /// <param name="ItemName">the name of the selected menu item</param>
        public override void EA_MenuClick(EA.Repository rep, string Location, string MenuName, string ItemName)
        {
            EA.ObjectType oType = rep.GetContextItemType();
            EA.Diagram diaCurrent = rep.GetCurrentDiagram();
            EA.Connector conCurrent = null;
            EA.Element el = null;

            if (diaCurrent != null) 
            {
                conCurrent = diaCurrent.SelectedConnector;
            }
            switch (ItemName)
            {

                    case menuShowWindow:
                    ShowAddinControlWindows();
                    
                    
                    break;
                case menuAbout:

                    About fAbout = new About();
                    fAbout.lblTabName.Text = release;
                    fAbout.ShowDialog();
                    break;

                    // Line style: Lateral Horizontal 
                case menuChangeXmlFile:
                     EaService.setNewXmlPath(rep);
                     break;
                // Line style: Lateral Horizontal 
                case menuLineStyleDiaLH:
                     EaService.setLineStyle(rep, "LH");
                   
                    break;
                // Line style: Lateral Vertical 
                case menuLineStyleDiaLV:
                    // all connections of diagram
                    EaService.setLineStyle(rep, "LV");
                    break;
                // Line style: Tree Vertical 
                case menuLineStyleDiaTV:
                    EaService.setLineStyle(rep, "V");
                    
                    break;

                // Line style: Tree Horizental 
                case menuLineStyleDiaTH:
                    EaService.setLineStyle(rep, "H");
                    
                    break;
                // Line style: Orthogonal square 
                case menuLineStyleDiaOS:
                    EaService.setLineStyle(rep, "OS");
                    
                    break;


                //if (ItemName == menuHelp)
                //{
                //    Help fHelp = new Help();
                //    fHelp.ShowDialog();
                //    return;
                //}
                case menuUnlockDiagram:
                    if (oType.Equals(EA.ObjectType.otDiagram))
                    {
                        try
                        {
                            string sql = @"update t_diagram set locked = 0" +
                           " where diagram_ID = " + diaCurrent.DiagramID.ToString();
                            rep.Execute(sql);
                            // reload view
                            rep.ReloadDiagram(diaCurrent.DiagramID);
                        }
                        catch { }
                    }

                    break;

                // Start specification (file parameter)
                case  menuShowSpecification:
                      EaService.showSpecification(rep);

                    break;

                // Create Interaction for selected operation or class (all operations)
                case   menuCreateInteractionForOperation:
                    // Check selected Elements in tree
                    if (oType.Equals(EA.ObjectType.otMethod))
                    {
                        EA.Method m = (EA.Method)rep.GetContextObject();
                        // test multiple selection

                        // Create Activity
                        Appl.createInteractionForOperation(rep, m);

                    }
                    if (oType.Equals(EA.ObjectType.otElement))
                    {
                        EA.Element cls = (EA.Element)rep.GetContextObject();
                        // over all operations of class
                        foreach (EA.Method m in cls.Methods)
                        {
                            // Create Activity
                            Appl.createInteractionForOperation(rep, m);

                        }
                    }

                    break;

                // Create Interaction for selected operation or class (all operations)
                case menuCreateStateMachineForOperation:
                    // Check selected Elements in tree
                    if (oType.Equals(EA.ObjectType.otMethod))
                    {
                        EA.Method m = (EA.Method)rep.GetContextObject();
                        // test multiple selection

                        // Create State Machine
                        Appl.createStateMachineForOperation(rep, m);

                    }
                   break;



                case menuLocateCompositeElementorDiagram:
                   EaService.navigateComposite(rep);
                    break;
                    
                // 
                case menuCorrectType:
                    if (oType.Equals(EA.ObjectType.otAttribute))
                    {
                        EA.Attribute a = (EA.Attribute)rep.GetContextObject();

                        Util.updateAttribute(rep, a);
                    }

                    if (oType.Equals(EA.ObjectType.otMethod))
                    {
                        EA.Method m = (EA.Method)rep.GetContextObject();

                        Util.updateMethod(rep, m);
                    }
                    if (oType.Equals(EA.ObjectType.otElement))
                    {
                        el = (EA.Element)rep.GetContextObject();
                        Util.updateClass(rep, el);
                    }
                    if (oType.Equals(EA.ObjectType.otPackage))
                    {
                        EA.Package pkg = (EA.Package)rep.GetContextObject();
                        Util.updatePackage(rep, pkg);
                    }
                    break;

                
                case menuCreateActivityForOperation:
                    EaService.CreateActivityForOperation(rep);

                    break;

                    // get Parameter for Activity
                case menuUpdateOperationParameter:
                    EaService.UpdateActivityParameter(rep);
                    break;

                case menuUpdateActionPin:
                    if (oType.Equals(EA.ObjectType.otPackage))
                    {
                        EA.Package pkg = (EA.Package)rep.GetContextObject();
                        ActionPin.updateActionPinForPackage(rep, pkg);
                    }
                    if (oType.Equals(EA.ObjectType.otElement))
                    {
                        el = (EA.Element)rep.GetContextObject();
                        ActionPin.updateActionPinForElement(rep, el);
                    }
                    break;
                

                case menuAddLinkedDiagramNote:
                    EaService.addDiagramNote(rep); 
                               
                    break;

                case menuAddLinkedNote:
                    EaService.addElementNote(rep);

                    break;

                case menuLocateType:
                    EaService.locateType(rep);
                    
                    break;

                case menuUsage:
                    EaService.findUsage(rep);
                    
                    break;

                case menuPasteLinksFromClipboard:
                    if (oType.Equals(EA.ObjectType.otElement)) // only Element
                    {
                        el = (EA.Element)rep.GetContextObject();
                        string conStr = Clipboard.GetText();  // get Clippboard
                        if (conStr.StartsWith("{") && conStr.Substring(37,1)=="}" && conStr.EndsWith("\r\n")) {
                            rep.CreateOutputTab("DEBUG");
                            rep.EnsureOutputVisible("DEBUG");
                            int countError = 0;
                            int countInserted = 0;
                            string[] conStrList = conStr.Split('\n');
                            foreach (string line in conStrList)
                            {
                                if (line.Length > 38)
                                {
                                    string guid = line.Substring(0, 38);
                                    EA.Connector con = rep.GetConnectorByGuid(guid);

                                    // Client/Source
                                    if (line.Substring(38, 1) == "C")
                                    {
                                        try
                                        {
                                            con.ClientID = el.ElementID;
                                            con.Update();
                                            countInserted = countInserted + 1;
                                        }
                                        catch
                                        {
                                            countError = countError + 1;
                                            EA.Element el1 = rep.GetElementByID(con.SupplierID);
                                            string fText = String.Format("Error Name {0}, Error={1}", el1.Name, con.GetLastError());
                                            rep.WriteOutput("Debug", fText, 0);
                                        }
                                    }
                                    //Supplier / Target
                                    if (line.Substring(38, 1) == "S")
                                    {
                                        try
                                        {

                                            con.SupplierID = el.ElementID;
                                            con.Update();
                                            countInserted = countInserted + 1;
                                        }
                                        catch
                                        {
                                            countError = countError + 1;
                                            EA.Element el1 = rep.GetElementByID(con.ClientID);
                                            string fText = String.Format("Error Name {0}, Error={1}", el1.Name, con.GetLastError());
                                            rep.WriteOutput("Debug",fText,0);
                                        }

                                    }
                                }
                            }
                            // update diagram
                            EA.Diagram dia = rep.GetCurrentDiagram();
                            if (dia != null)
                            {
                                try
                                {
                                    dia.Update();
                                    rep.ReloadDiagram(dia.DiagramID);
                                }
                                catch
                                {
                                }
                                
                            }
                            MessageBox.Show(string.Format("Kopiert:{0}\r\nErrors:{1}", countInserted,countError));
                        }

                        

                    }
                    break;

                case menuCopyGUIDToClipboard:
                   EaService.copyGuidSqlToClipboard(rep);
                   break;


                // put on Clipboard
                // 'ConnectorGUID', 'Client' if element is a client/source in this dependency
                // 'ConnectorGUID', 'Supplier' if element is a supplier/target in this dependency

                case menuCopyLinksToClipboard:
                    if (oType.Equals(EA.ObjectType.otElement)) // only Element
                    {
                        el = (EA.Element)rep.GetContextObject();
                        string conStr = "";
                        foreach (EA.Connector con in el.Connectors)
                        {
                            conStr = conStr + con.ConnectorGUID;
                            // check if client or supllier
                            if (con.ClientID == el.ElementID) conStr = conStr + "Client  \r\n";
                            if (con.SupplierID == el.ElementID) conStr = conStr + "Supplier\r\n";

                        }
                        if (conStr.Length > 0)
                        {
                            Clipboard.SetText(conStr);
                        }
                    }
                    break;

                case menuDisplayMethodDefinition:
                    EaService.DisplayOperationForSelectedElement(rep, EaService.displayMode.Method);
                    break;

                case menuDisplayBehavior:
                    EaService.DisplayOperationForSelectedElement(rep, EaService.displayMode.Behavior);
                    break;

                
            }
        }

       


        #region ShowAddinControlWindows
        private void ShowAddinControlWindows()
        {
            if (m_MyControlGUI == null)
            {

                try
                {

                    string tabName = m_AddinSettings.productName;
                    m_MyControlGUI = (AddinControlGUI)m_repository.AddWindow(tabName, "hoTools.ActiveXGUI");
                    if (m_MyControlGUI == null)
                    {
                        MessageBox.Show("Unable to start ActiveX window 'hoTools.ActiveXGUI'");
                    }

                    AddinControlGUI = m_MyControlGUI; // static

                    // Find & Replace
                    m_FindAndReplace = (FindAndReplaceGUI)m_repository.AddWindow("Find&..", "hoTools.FindAndReplaceGUI");
                    if (m_FindAndReplace == null)
                    {
                        MessageBox.Show("Unable to start ActiveX window 'hoTools.FindAndReplaceGUI'");
                    }
                    FindAndReplaceGUI = m_FindAndReplace; // static

                    
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
                //m_MyControl.mStr_UserText = "testText";
                //m_MyControl.UserText = "zweittext";

                // ActiveX Main
                if (m_MyControlGUI != null)
                {
                    m_MyControlGUI.repository = m_repository;
                    m_MyControlGUI.Release = "V" + release;
                    m_MyControlGUI.addinSettings = this.m_AddinSettings;
                }
                // Find & Replace
                if (m_FindAndReplace != null)
                {
                    m_FindAndReplace.repository = m_repository;
                    m_FindAndReplace.Release = "V" + release;
                    m_FindAndReplace.addinSettings = this.m_AddinSettings;
                }

               
            }
        }
        #endregion

       
          // display behavior or definition for selected element
        private static void DisplayOperationForSelectedElement(EA.Repository rep, displayMode showBehavior)
        {
            EA.ObjectType oType = rep.GetContextItemType();
            // Method found
            if (oType.Equals(EA.ObjectType.otMethod))
            {
                // display behavior for method
                Appl.DisplayBehaviorForOperation(rep, (EA.Method)rep.GetContextObject());

            }
            if (oType.Equals(EA.ObjectType.otDiagram))
            {
                // find parent element
                EA.Diagram dia = (EA.Diagram)rep.GetContextObject();
                if (dia.ParentID > 0)
                {
                    // find parent element
                    EA.Element parentEl = rep.GetElementByID(dia.ParentID);
                    //
                    locateOperationFromBehavior(rep, parentEl, showBehavior);
                }
                else
                {
                    // open diagram
                    rep.OpenDiagram(dia.DiagramID);
                }
            }


            // Connector / Message found
            if (oType.Equals(EA.ObjectType.otConnector))
            {
                EA.Connector con = (EA.Connector)rep.GetContextObject();
                if (con.Type.Equals("StateFlow"))
                {

                    EA.Method m = Util.getOperationFromConnector(rep, con);
                    if (m != null)
                    {
                        if (showBehavior.Equals(displayMode.Behavior))
                        {
                            Appl.DisplayBehaviorForOperation(rep, m);
                        }
                        else
                        {
                            rep.ShowInProjectView(m);
                        }

                    }


                }

                if (con.Type.Equals("Sequence"))
                {
                    // If name is of the form: OperationName(..) the the operation is associated to an method
                    string opName = con.Name;
                    if (opName.EndsWith(")"))
                    {
                        // extract the name
                        int pos = opName.IndexOf("(");
                        opName = opName.Substring(0, pos);
                        EA.Element el = rep.GetElementByID(con.SupplierID);
                        // find operation by name
                        foreach (EA.Method op in el.Methods)
                        {
                            if (op.Name == opName)
                            {
                                rep.ShowInProjectView(op);
                                //Appl.DisplayBehaviorForOperation(Repository, op);
                                return;
                            }
                        }
                        if ((el.Type.Equals("Sequence") || el.Type.Equals("Object")) && el.ClassfierID > 0)
                        {
                            el = (EA.Element)rep.GetElementByID(el.ClassifierID);
                            foreach (EA.Method op in el.Methods)
                            {
                                if (op.Name == opName)
                                {
                                    if (showBehavior.Equals(displayMode.Behavior))
                                    {
                                        Appl.DisplayBehaviorForOperation(rep, op);
                                    }
                                    else
                                    {
                                        rep.ShowInProjectView(op);
                                    }

                                }
                            }
                        }

                    }
                }
            }

            // Element
            if (oType.Equals(EA.ObjectType.otElement))
            {
                EA.Element el = (EA.Element)rep.GetContextObject();

                if (el.Type.Equals("Activity"))
                {
                    // Open Behavior for Activity
                    Util.OpenBehaviorForElement(rep, el);


                }
               if (el.Type.Equals("State"))
                {
                    // get operations
                    foreach (EA.Method m in el.Methods)
                    {
                        // display behaviors for methods
                        Appl.DisplayBehaviorForOperation(rep, m);
                    }
                }
                if (el.Type.Equals("Action"))
                {
                    foreach (EA.CustomProperty custproperty in el.CustomProperties)
                    {
                        if (custproperty.Name.Equals("kind") && custproperty.Value.Equals("CallOperation"))
                        {
                            showFromElement(rep, el, showBehavior);
                        }
                        if (custproperty.Name.Equals("kind") && custproperty.Value.Equals("CallBehavior"))
                        {
                            el = rep.GetElementByID(el.ClassfierID);
                            Util.OpenBehaviorForElement(rep, el);
                        }
                    }

                }
                if (el.Type.Equals("Activity") || el.Type.Equals("StateMachine") || el.Type.Equals("Interaction"))
                {
                    locateOperationFromBehavior(rep, el, showBehavior);
                }
            }
        }

        private static void showFromElement(EA.Repository rep, EA.Element el, displayMode showBehavior)
        {
            EA.Method method = Util.getOperationFromAction(rep, el);
            if (method != null)
            {
                if (showBehavior.Equals(displayMode.Behavior))
                {
                    Appl.DisplayBehaviorForOperation(rep, method);
                }
                else
                {
                    rep.ShowInProjectView(method);
                }
            }
        }

        private static void locateOperationFromBehavior(EA.Repository rep, EA.Element el, displayMode showBehavior)
        {
            EA.Method method = Util.getOperationFromBrehavior(rep, el);
            if (method != null)
            {
                if (showBehavior.Equals(displayMode.Behavior))
                {
                    Appl.DisplayBehaviorForOperation(rep, method);
                }
                else
                {
                    rep.ShowInProjectView(method);
                }
            }
        }
        private static void BehaviorForOperation(EA.Repository rep, EA.Method method)
        {
            string behavior = method.Behavior;
            if (behavior.StartsWith("{") & behavior.EndsWith("}"))
            {
                // get object according to behavior
                EA.Element el = rep.GetElementByGuid(behavior);
                // Activity
                if (el == null) { }
                else
                {
                    if (el.Type.Equals("Activity") || el.Type.Equals("Interaction") || el.Type.Equals("StateMachine"))
                    {
                        Util.OpenBehaviorForElement(rep, el);
                    }
                }
            }
        }
        //---------------------------------------------------------------------------------------------------------------
        // linestyle
        // LH = "Line Style: Lateral Horizontal";
        // LV = "Line Style: Lateral Vertical";
        // TH  = "Line Style: Tree Horizontal";
        // TV = "Line Style: Tree Vertical";
        // OS = "Line Style: Orthogonal Square";
        // OR =              Orthogonal Round
        // A =               Automatic
        // D =               Direct
        // C =               Customer

        void btnLH_Click(object sender, EventArgs e)
        {
            EaService.setLineStyle(m_repository, "LH");
        }
        void btnLV_Click(object sender, EventArgs e)
        {
            EaService.setLineStyle(m_repository, "LV");
        }
        void btnTH_Click(object sender, EventArgs e)
        {
            EaService.setLineStyle(m_repository, "H");
        }
        void btnTV_Click(object sender, EventArgs e)
        {
            EaService.setLineStyle(m_repository, "V");
        }
        void btnOS_Click(object sender, EventArgs e)
        {
            EaService.setLineStyle(m_repository, "OS");
        }
        void btnOR_Click(object sender, EventArgs e)
        {
            EaService.setLineStyle(m_repository, "OR");
        }
        void btnA_Click(object sender, EventArgs e)
        {
            EaService.setLineStyle(m_repository, "A");
        }
        void btnD_Click(object sender, EventArgs e)
        {
            EaService.setLineStyle(m_repository, "D");
        }
        void btnC_Click(object sender, EventArgs e)
        {
            EaService.setLineStyle(m_repository, "C");
        }

        void btnComposite_Click(object sender, EventArgs e)
        {
            EaService.navigateComposite(m_repository);
        }






        void btnDisplayBehavior_Click(object sender, EventArgs e)
        {
            EaService.DisplayOperationForSelectedElement(m_repository, EaService.displayMode.Behavior);
        }
        void btnLocateOperation_Click(object sender, EventArgs e)
        {
            EaService.DisplayOperationForSelectedElement(m_repository, EaService.displayMode.Method);
        }
        void btnAddElementNote_Click(object sender, EventArgs e)
        {
            EaService.addElementNote(m_repository);
        }
        void btnAddDiagramNote_Click(object sender, EventArgs e)
        {
            EaService.addDiagramNote(m_repository);
        }

        void btnLocateType_Click(object sender, EventArgs e)
        {
            EaService.locateType(m_repository);
        }
        void btnFindUsage_Click(object sender, EventArgs e)
        {
            EaService.findUsage(m_repository);
        }
        void btnDisplaySpecification_Click(object sender, EventArgs e)
        {
            EaService.showSpecification(m_repository);
        }

               
        /// <summary>
        /// Called when EA start model validation. Just shows a message box
        /// </summary>
        /// <param name="rep">the repository</param>
        /// <param name="Args">the arguments</param>
        public override void EA_OnStartValidation(EA.Repository rep, object Args)
        {
            MessageBox.Show("Validation started");
        }
        /// <summary>
        /// Called when EA ends model validation. Just shows a message box
        /// </summary>
        /// <param name="rep">the repository</param>
        /// <param name="Args">the arguments</param>
        public override void EA_OnEndValidation(EA.Repository rep, object Args)
        {
            MessageBox.Show("Validation ended");
        }
        /// <summary>
        /// called when the selected item changes
        /// This operation will show the guid of the selected element in the eaControl
        /// </summary>
        /// <param name="rep">the EA.Repository</param>
        /// <param name="GUID">the guid of the selected item</param>
        /// <param name="ot">the object type of the selected item</param>
        public override void EA_OnContextItemChanged(EA.Repository rep, string GUID, EA.ObjectType ot)
        { }
        /// <summary>
        /// Say Hello to the world
        /// </summary>

    }
}
