﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Office.Core;
using Microsoft.Vbe.Interop;
using Ninject;
using Ninject.Modules;
using Rubberduck.Navigation;
using Rubberduck.UI;
using Rubberduck.UI.Command;

namespace Rubberduck.Root
{
    public class CommandBarsModule : NinjectModule
    {
        private readonly IKernel _kernel;

        public CommandBarsModule(IKernel kernel)
        {
            _kernel = kernel;
        }

        public override void Load()
        {
            BindCommandsToMenuItems();

            ConfigureRubberduckMenu();
            ConfigureCodePaneContextMenu();
            ConfigureFormDesignerContextMenu();
            ConfigureFormDesignerControlContextMenu();
            ConfigureProjectExplorerContextMenu();
        }

        private void ConfigureRubberduckMenu()
        {
            const int windowMenuId = 30009;
            var parent = _kernel.Get<VBE>().CommandBars["Menu Bar"].Controls;
            var beforeIndex = FindRubberduckMenuInsertionIndex(parent, windowMenuId);

            var items = GetRubberduckMenuItems();
            BindParentMenuItem<RubberduckParentMenu, MainMenuAttribute>(parent, beforeIndex, items);
        }

        private void ConfigureCodePaneContextMenu()
        {
            const int listMembersMenuId = 2529;
            var parent = _kernel.Get<VBE>().CommandBars["Code Window"].Controls;
            var beforeIndex = parent.Cast<CommandBarControl>().First(control => control.Id == listMembersMenuId).Index;

            var items = GetCodePaneContextMenuItems();
            BindParentMenuItem<RubberduckParentMenu, CodePaneContextMenuAttribute>(parent, beforeIndex, items);
        }

        private void ConfigureFormDesignerContextMenu()
        {
            const int viewCodeMenuId = 2558;
            var parent = _kernel.Get<VBE>().CommandBars["MSForms"].Controls;
            var beforeIndex = parent.Cast<CommandBarControl>().First(control => control.Id == viewCodeMenuId).Index;

            var items = GetFormDesignerContextMenuItems();
            BindParentMenuItem<FormDesignerContextParentMenu, FormDesignerContextMenuAttribute>(parent, beforeIndex, items);
        }

        private void ConfigureFormDesignerControlContextMenu()
        {
            const int viewCodeMenuId = 2558;
            var parent = _kernel.Get<VBE>().CommandBars["MSForms Control"].Controls;
            var beforeIndex = parent.Cast<CommandBarControl>().First(control => control.Id == viewCodeMenuId).Index;

            var items = GetFormDesignerContextMenuItems();
            BindParentMenuItem<FormDesignerControlContextParentMenu, FormDesignerControlContextMenuAttribute>(parent, beforeIndex, items);
        }

        private void ConfigureProjectExplorerContextMenu()
        {
            const int projectPropertiesMenuId = 2578;
            var parent = _kernel.Get<VBE>().CommandBars["Project Window"].Controls;
            var beforeIndex = parent.Cast<CommandBarControl>().First(control => control.Id == projectPropertiesMenuId).Index;

            var items = GetProjectWindowContextMenuItems();
            BindParentMenuItem<ProjectWindowContextParentMenu, ProjectWindowContextMenuAttribute>(parent, beforeIndex, items);
        }

        private void BindParentMenuItem<TParentMenu, TAttribute>(CommandBarControls parent, int beforeIndex, IEnumerable<IMenuItem> items)
        {
            _kernel.Bind<IParentMenuItem>().To(typeof(TParentMenu))
                .WhenTargetHas(typeof(TAttribute))
                .InSingletonScope()
                .WithConstructorArgument("items", items)
                .WithConstructorArgument("beforeIndex", beforeIndex)
                .WithPropertyValue("Parent", parent);
        }

        private static int FindRubberduckMenuInsertionIndex(CommandBarControls controls, int beforeId)
        {
            for (var i = 1; i <= controls.Count; i++)
            {
                if (controls[i].BuiltIn && controls[i].Id == beforeId)
                {
                    return i;
                }
            }

            return controls.Count;
        }

        private void BindCommandsToMenuItems()
        {
            _kernel.Bind<ICommand<NavigateCodeEventArgs>>().To<NavigateCommand>().InSingletonScope();
            _kernel.Bind<IDeclarationNavigator>().To<NavigateAllImplementations>().WhenTargetHas<FindImplementationsAttribute>().InSingletonScope();
            _kernel.Bind<IDeclarationNavigator>().To<NavigateAllReferences>().WhenTargetHas<FindReferencesAttribute>().InSingletonScope();

            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.Namespace != null && type.Namespace.StartsWith(typeof(ICommand).Namespace ?? string.Empty))
                .ToList();

            // note: ICommand naming convention: [Foo]Command
            var commands = types.Where(type => type.IsClass && type.GetInterfaces().Contains(typeof(ICommand)) && type.Name.EndsWith("Command"));
            foreach (var command in commands)
            {
                var commandName = command.Name.Substring(0, command.Name.Length - "Command".Length);
                try
                {
                    // note: ICommandMenuItem naming convention for [Foo]Command: [Foo][*]CommandMenuItem
                    var item = types.SingleOrDefault(type => type.Name.StartsWith(commandName) && type.Name.EndsWith("CommandMenuItem"));
                    if (item != null)
                    {
                        _kernel.Bind(item).ToSelf().InSingletonScope();
                        _kernel.Bind<ICommand>().To(command).WhenInjectedInto(item).InSingletonScope();
                    }
                }
                catch (InvalidOperationException exception)
                {
                    // rename one of the classes, "FooCommand" is expected to match exactly 1 "FooBarXyzCommandMenuItem"
                }
            }
        }

        private IEnumerable<IMenuItem> GetRubberduckMenuItems()
        {
            return new[]
            {
                _kernel.Get<AboutCommandMenuItem>(),
                _kernel.Get<OptionsCommandMenuItem>(), 
                _kernel.Get<RunCodeInspectionsCommandMenuItem>(),
                _kernel.Get<ShowSourceControlPanelCommandMenuItem>(), 
                GetUnitTestingParentMenu(),
                GetRefactoringsParentMenu(),
                GetNavigateParentMenu(),
            };
        }

        private IMenuItem GetUnitTestingParentMenu()
        {
            var items = new IMenuItem[]
            {
                _kernel.Get<RunAllTestsUnitTestingCommandMenuItem>(), 
                _kernel.Get<TestExplorerUnitTestingCommandMenuItem>(), 
            };

            return new UnitTestingParentMenu(items);
        }

        private IMenuItem GetRefactoringsParentMenu()
        {
            var items = new IMenuItem[]
            {
                _kernel.Get<RefactorRenameCommandMenuItem>(), 
                _kernel.Get<RefactorExtractMethodCommandMenuItem>(), 
                _kernel.Get<RefactorReorderParametersCommandMenuItem>(), 
                _kernel.Get<RefactorRemoveParametersCommandMenuItem>(), 
            };

            return new RefactoringsParentMenu(items);
        }

        private IMenuItem GetNavigateParentMenu()
        {
            var items = new IMenuItem[]
            {
                _kernel.Get<CodeExplorerCommandMenuItem>(), 
                _kernel.Get<ToDoExplorerCommandMenuItem>(), 
                _kernel.Get<FindSymbolCommandMenuItem>(),
                _kernel.Get<FindAllReferencesCommandMenuItem>(),
                _kernel.Get<FindAllImplementationsCommandMenuItem>(),
            };

            return new NavigateParentMenu(items);
        }

        private IEnumerable<IMenuItem> GetCodePaneContextMenuItems()
        {
            return new[]
            {
                GetRefactoringsParentMenu(),
                GetNavigateParentMenu()
            };
        }

        private IEnumerable<IMenuItem> GetFormDesignerContextMenuItems()
        {
            return new[]
            {
                _kernel.Get<RefactorRenameCommandMenuItem>(), 
            };
        }

        private IEnumerable<IMenuItem> GetProjectWindowContextMenuItems()
        {
            return new[]
            {
                GetNavigateParentMenu(),
            };
        }
    }
}