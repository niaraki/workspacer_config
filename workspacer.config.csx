// Production
#r "D:\Program Files\workspacer\workspacer.Shared.dll"
#r "D:\Program Files\workspacer\plugins\workspacer.Bar\workspacer.Bar.dll"
#r "D:\Program Files\workspacer\plugins\workspacer.Gap\workspacer.Gap.dll"
#r "D:\Program Files\workspacer\plugins\workspacer.ActionMenu\workspacer.ActionMenu.dll"
#r "D:\Program Files\workspacer\plugins\workspacer.FocusIndicator\workspacer.FocusIndicator.dll"

using System;
using System.Collections.Generic;
using System.Linq;
using workspacer;
using workspacer.Bar;
using workspacer.Bar.Widgets;
using workspacer.Gap;
using workspacer.ActionMenu;
using workspacer.FocusIndicator;

return new Action<IConfigContext>((IConfigContext context) =>
{
    /* Variables */
    var fontSize = 10;
    var barHeight = 19;
    var fontName = "IosvekaTerm";
    var background = new Color(0, 0, 0);

    /* Config */
    context.CanMinimizeWindows = false;

    /* Gap */
    var gap = barHeight - 8;
    var gapPlugin = context.AddGap(new GapPluginConfig() { InnerGap = gap, OuterGap = gap / 2, Delta = gap / 2 });

    /* Bar */
    context.AddBar(new BarPluginConfig()
    {
        FontSize = fontSize,
        BarHeight = barHeight,
        FontName = fontName,
        DefaultWidgetBackground = background,
        LeftWidgets = () => new IBarWidget[]
        {
            new TextWidget(".:: NinjaCoder ::. "), new WorkspaceWidget()
        },
        RightWidgets = () => new IBarWidget[]
        {
            new TextWidget("<<"),
            new TitleWidget() {
                IsShortTitle = true
            },
            new TextWidget(">>"),
            new TextWidget(".:::  🔋"), new BatteryWidget(),
            new TimeWidget(1000, " ::: ⏲️ HH:mm   📅 dd-MMM  :::"), 
            new ActiveLayoutWidget(),
            new TextWidget(":::."),
            new FocusedMonitorWidget(){
                FocusedText = "🦅",
                UnfocusedText = ""
            },
            new TextWidget(" ")
        }
    });

    /* Bar focus indicator */
    context.AddFocusIndicator();

    /* Default layouts */
    Func<ILayoutEngine[]> defaultLayouts = () => new ILayoutEngine[]
    {
        new TallLayoutEngine(),
        new VertLayoutEngine(),
        new HorzLayoutEngine(),
        new FullLayoutEngine(),
    };

    context.DefaultLayouts = defaultLayouts;

    /* Workspaces */
    // Array of workspace names and their layouts
    (string, ILayoutEngine[])[] workspaces =
    {
        ("(1)Main",new ILayoutEngine[] { new TallLayoutEngine(), new FullLayoutEngine() }),
        ("(2)Web", new ILayoutEngine[] { new HorzLayoutEngine(), new FullLayoutEngine()}),
        ("(3)EChat", new ILayoutEngine[] { new HorzLayoutEngine(), new FullLayoutEngine()}),
        ("(4)Files",new ILayoutEngine[] { new HorzLayoutEngine(), new FullLayoutEngine()}),
        ("(5)Docs", defaultLayouts()),
        ("(6)Media", defaultLayouts()),
        ("(7)VM",new ILayoutEngine[] { new FullLayoutEngine()}),
        ("(8)Code-A",new ILayoutEngine[] { new TallLayoutEngine(), new FullLayoutEngine() }),
        ("(9)Code-B",new ILayoutEngine[] { new TallLayoutEngine(), new FullLayoutEngine() }),
        ("(8)Other", defaultLayouts()),
    };

    foreach ((string name, ILayoutEngine[] layouts) in workspaces)
    {
        context.WorkspaceContainer.CreateWorkspace(name, layouts);
    }

    /* Filters : I don't want to manage these windows through workspacer */
    context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("1password.exe"));
    context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("putty.exe"));
    context.WindowRouter.AddFilter((window) => !window.ProcessFileName.Equals("TortoiseGitProc.exe"));
    context.WindowRouter.AddFilter((window) => !window.Title.Contains("Snipping"));
    context.WindowRouter.AddFilter((window) => !window.Title.Contains("Discovered"));
    context.WindowRouter.AddFilter((window) => !window.Title.ToLower().Contains("% complete"));
    context.WindowRouter.AddFilter((window) => !window.Title.StartsWith("Delete"));
    context.WindowRouter.AddFilter((window) => !window.Title.StartsWith("Folder"));
    context.WindowRouter.AddFilter((window) => !window.Title.Contains("Info"));
    context.WindowRouter.AddFilter((window) => !window.Title.Contains("Warning"));
    context.WindowRouter.AddFilter((window) => !window.Class.Equals("ShellTrayWnd"));

    /* Routes */
    context.WindowRouter.RouteProcessName("Code", "(8)Code-A");
    context.WindowRouter.RouteProcessName("notepad++", "(9)Code-B");
    context.WindowRouter.RouteProcessName("chrome", "(2)Web");
    context.WindowRouter.RouteProcessName("firefox", "(2)Web");
    context.WindowRouter.RouteProcessName("explorer", "(4)Files");
    context.WindowRouter.RouteTitle("Movies & TV", "(6)Media");
    context.WindowRouter.RouteTitle("Groove Music", "(6)Media");
    context.WindowRouter.RouteTitle("YouTube", "(6)Media");
    context.WindowRouter.RouteProcessName("VirtualBox", "(7)VM");

    /* Action menu */
    var actionMenu = context.AddActionMenu(new ActionMenuPluginConfig()
    {
        RegisterKeybind = false,
        MenuHeight = barHeight,
        FontSize = fontSize,
        FontName = fontName,
        Background = background,
    });

    /* Action menu builder */
    Func<ActionMenuItemBuilder> createActionMenuBuilder = () =>
    {
        var menuBuilder = actionMenu.Create();

        // Switch to workspace
        menuBuilder.AddMenu("switch", () =>
        {
            var workspaceMenu = actionMenu.Create();
            var monitor = context.MonitorContainer.FocusedMonitor;
            var workspaces = context.WorkspaceContainer.GetWorkspaces(monitor);

            Func<int, Action> createChildMenu = (workspaceIndex) => () =>
            {
                context.Workspaces.SwitchMonitorToWorkspace(monitor.Index, workspaceIndex);
            };

            int workspaceIndex = 0;
            foreach (var workspace in workspaces)
            {
                workspaceMenu.Add(workspace.Name, createChildMenu(workspaceIndex));
                workspaceIndex++;
            }

            return workspaceMenu;
        });

        // Move window to workspace
        menuBuilder.AddMenu("move", () =>
        {
            var moveMenu = actionMenu.Create();
            var focusedWorkspace = context.Workspaces.FocusedWorkspace;

            var workspaces = context.WorkspaceContainer.GetWorkspaces(focusedWorkspace).ToArray();
            Func<int, Action> createChildMenu = (index) => () => { context.Workspaces.MoveFocusedWindowToWorkspace(index); };

            for (int i = 0; i < workspaces.Length; i++)
            {
                moveMenu.Add(workspaces[i].Name, createChildMenu(i));
            }

            return moveMenu;
        });

        // Rename workspace
        menuBuilder.AddFreeForm("rename", (name) =>
        {
            context.Workspaces.FocusedWorkspace.Name = name;
        });

        // Create workspace
        menuBuilder.AddFreeForm("create workspace", (name) =>
        {
            context.WorkspaceContainer.CreateWorkspace(name);
        });

        // Delete focused workspace
        menuBuilder.Add("close", () =>
        {
            context.WorkspaceContainer.RemoveWorkspace(context.Workspaces.FocusedWorkspace);
        });

        // Workspacer
        menuBuilder.Add("toggle keybind helper", () => context.Keybinds.ShowKeybindDialog());
        menuBuilder.Add("toggle enabled", () => context.Enabled = !context.Enabled);
        menuBuilder.Add("restart", () => context.Restart());
        menuBuilder.Add("quit", () => context.Quit());

        return menuBuilder;
    };
    var actionMenuBuilder = createActionMenuBuilder();

    /* Keybindings */
    Action setKeybindings = () =>
    {
        KeyModifiers win = KeyModifiers.Win;
        KeyModifiers alt = KeyModifiers.Alt;
        KeyModifiers altCtrl = KeyModifiers.Alt | KeyModifiers.Control;
        KeyModifiers altCtrlShift = KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Shift;
        KeyModifiers altShift = KeyModifiers.Alt | KeyModifiers.Shift;

        IKeybindManager manager = context.Keybinds;

        var workspaces = context.Workspaces;

        manager.Subscribe(MouseEvent.LButtonDown, () => workspaces.SwitchFocusedMonitorToMouseLocation());

        // H, L keys
        manager.Unsubscribe(alt, Keys.H);
        manager.Unsubscribe(alt, Keys.L);

        manager.Subscribe(altCtrl, Keys.H, () => workspaces.FocusedWorkspace.ShrinkPrimaryArea(), "shrink primary area");
        manager.Subscribe(altCtrl, Keys.L, () => workspaces.FocusedWorkspace.ExpandPrimaryArea(), "expand primary area");

        manager.Subscribe(alt, Keys.H, () => workspaces.FocusedWorkspace.FocusNextWindow(), "focus next windows");
        manager.Subscribe(alt, Keys.L, () => workspaces.FocusedWorkspace.FocusPreviousWindow(), "focus previous windows");

        manager.Subscribe(altShift, Keys.H, () => workspaces.FocusedWorkspace.SwapFocusAndNextWindow(), "swap the focuesed window with the next window");
        manager.Subscribe(altShift, Keys.L, () => workspaces.FocusedWorkspace.SwapFocusAndPreviousWindow(), "swap the focuesed window with the previous window");

        manager.Subscribe(altCtrlShift, Keys.H, () => workspaces.MoveFocusedWindowToPreviousMonitor(), "move focused window to next monitor");
        manager.Subscribe(altCtrlShift, Keys.L, () => workspaces.MoveFocusedWindowToNextMonitor(), "move focused window to previous monitor");

        // J, K
        manager.Unsubscribe(alt, Keys.Space);
        manager.Unsubscribe(alt, Keys.J);
        manager.Unsubscribe(alt, Keys.K);
        manager.Subscribe(alt, Keys.J, () => workspaces.FocusedWorkspace.NextLayoutEngine(), "switch to the next layout engine");
        manager.Subscribe(alt, Keys.K, () => workspaces.FocusedWorkspace.PreviousLayoutEngine(), "switch to the previous layout engine");

        // Add, Subtract keys
        manager.Subscribe(alt, Keys.Add, () => gapPlugin.IncrementInnerGap(), "increment inner gap");
        manager.Subscribe(alt, Keys.Subtract, () => gapPlugin.DecrementInnerGap(), "decrement inner gap");

        manager.Subscribe(altShift, Keys.Add, () => gapPlugin.IncrementOuterGap(), "increment outer gap");
        manager.Subscribe(altShift, Keys.Subtract, () => gapPlugin.DecrementOuterGap(), "decrement outer gap");

        // Monitor switch keybinds
        manager.Unsubscribe(alt , Keys.W); //switch to first monitor
        manager.Unsubscribe(alt , Keys.E); //switch to second monitor
        manager.Unsubscribe(alt , Keys.R); //switch to third monitor
        manager.Subscribe(alt, Keys.W, () => context.Workspaces.SwitchFocusedMonitor(0), "focus monitor 1");
        manager.Subscribe(alt, Keys.E, () => context.Workspaces.SwitchFocusedMonitor(1), "focus monitor 2");
        manager.Subscribe(alt, Keys.R, () => context.Workspaces.SwitchFocusedMonitor(2), "focus monitor 3");

        // Monitor jump keybinds
        manager.Unsubscribe(altShift, Keys.W); //Move focused windows to first monitor
        manager.Unsubscribe(altShift, Keys.E); //Move focused windows to second monitor
        manager.Unsubscribe(altShift, Keys.R); //Move focused windows to third monitor
        manager.Subscribe(altShift, Keys.W, () => context.Workspaces.MoveFocusedWindowToMonitor(0), "move focused window to monitor 1");
        manager.Subscribe(altShift, Keys.E, () => context.Workspaces.MoveFocusedWindowToMonitor(1), "move focused window to monitor 2");
        manager.Subscribe(altShift, Keys.R, () => context.Workspaces.MoveFocusedWindowToMonitor(2), "move focused window to monitor 3");

        // Other shortcuts
        manager.Subscribe(alt, Keys.D, () => actionMenu.ShowMenu(actionMenuBuilder), "show menu");
        manager.Subscribe(alt, Keys.I, () => context.ToggleConsoleWindow(), "toggle console window");

        manager.Unsubscribe(alt, Keys.Escape);
        manager.Subscribe(alt, Keys.Escape, () => context.Enabled = !context.Enabled, "toggle enabled/disabled");

        manager.Unsubscribe(alt, Keys.B);
        manager.Unsubscribe(alt, Keys.N);
        manager.Subscribe(alt, Keys.N, ()=> context.Workspaces.SwitchToNextWorkspace(),"switch to the next workspace");
        manager.Subscribe(alt, Keys.B, ()=> context.Workspaces.SwitchToPreviousWorkspace(),"switch to the previous workspace");
        
        manager.Unsubscribe(altShift , Keys.Q);
        manager.Subscribe(altShift, Keys.Q, () => context.Workspaces.FocusedWorkspace.CloseFocusedWindow(), "close the focused windows");

        manager.Unsubscribe(alt , Keys.Z);
        manager.Subscribe(alt, Keys.Z, () => context.Workspaces.FocusedWorkspace.ResetLayout(), "reset layout engine to default");

    };

    setKeybindings();
});
