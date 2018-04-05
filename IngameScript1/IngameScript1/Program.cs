using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set RuntimeInfo.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public static List<String> statusReport = new List<string>();
        public static List<String> listOfObjects = new List<string>();

        public void OpenCloseDoor(IMyBlockGroup blockGroup)
        {
           
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            blockGroup.GetBlocks(blocks);

            for (int i = 0; i < blocks.Count(); i++)
            {
                ITerminalAction action = blocks[i].GetActionWithName("Open");
                if (action != null)
                {
                    action.Apply(blocks[i]);
                }
            }
            
        }

        public void EchoText(String text, Boolean append)
        {
            try
            {
                IMyTextPanel lcd = GridTerminalSystem.GetBlockWithName("[DEBUG]") as IMyTextPanel;
                lcd.WritePublicText(text + System.Environment.NewLine, append);
            }catch (Exception e) { }
        }

        public class ProgramArgument {
            private String argument;
            private int argumentInt;

            public ProgramArgument(String argument, int argumentInt)
            {
                this.argument = argument;
                this.argumentInt = argumentInt;
            }

            public String getArgument()
            {
                return this.argument;
            }

            public int getArgumentInt()
            {
                return this.argumentInt;
            }

        }

        private List<ProgramArgument> ParseArgument(String argument)
        {
            StringBuilder argumentString = new StringBuilder();
            StringBuilder argumentInt = new StringBuilder();
            Boolean isArgument = false;
            Boolean isInt = false;
            List<ProgramArgument> result = new List<ProgramArgument>();

            for (int i = 0; i < argument.Length; i++)
            {
                if (!isInt & !isArgument & Char.IsLetter(argument[i]))
                {
                    argumentString = new StringBuilder();
                    argumentInt = new StringBuilder();
                    isArgument = true;
                }

                if (isArgument & !isInt)
                {
                    if (Char.IsLetter(argument[i]))
                    {
                        argumentString.Append(argument[i]);

                    }else if (argument[i] == ':')
                    {
                        isArgument = false;
                        isInt = true;
                    }
                }

                if (!isArgument & isInt)
                {
                    if (Char.IsDigit(argument[i]))
                    {
                        argumentInt.Append(argument[i]);
                    }
                }

                if (Char.IsWhiteSpace(argument[i]) || i == argument.Length - 1)
                {
                    isArgument = false;
                    isInt = false;
                    int argInt = -1;

                    if (argumentInt.Length > 0)
                    {
                        argInt = int.Parse(argumentInt.ToString());
                    }

                    result.Add(new ProgramArgument(argumentString.ToString(), argInt));
                }


            }

            return result;
        }

        void CleanFilters(IMyConveyorSorter sorter)
        {
            List<MyInventoryItemFilter> filters = new List<MyInventoryItemFilter>();
            sorter.GetFilterList(filters);

            foreach (MyInventoryItemFilter item in filters)
            {
                EchoText("Removed: " + item.ItemId.ToString(), true);
                sorter.RemoveItem(item);
            }
        }

        void AddFilter(IMyConveyorSorter sorter, List<String> filterName, bool groupFilter)
        {
            foreach (String s in filterName)
            {
                EchoText("Add: " + s, true);
                sorter.AddItem(new MyInventoryItemFilter(s, groupFilter));
            }
        }

        public enum SorterGroupFilters
        {
            ORE, COMPONENT, GUN, AMMO, INGOT
        }

        public static String getGroupFilterId(SorterGroupFilters e)
        {
            switch (e)
            {
                case SorterGroupFilters.ORE:
                    {
                        return "MyObjectBuilder_Ore/(null)";
                    }
                case SorterGroupFilters.COMPONENT:
                    {
                        return "MyObjectBuilder_Component/(null)";
                    }
                case SorterGroupFilters.GUN:
                    {
                        return "MyObjectBuilder_PhysicalGunObject/(null)";
                    }
                case SorterGroupFilters.AMMO:
                    {
                        return "MyObjectBuilder_AmmoMagazine/(null)";
                    }
                case SorterGroupFilters.INGOT:
                    {
                        return "MyObjectBuilder_Ingot/(null)";
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        public static List<String> getAllGroupFilters()
        {
            List<String> list = new List<string>();
            foreach (SorterGroupFilters f in (SorterGroupFilters[])Enum.GetValues(typeof(SorterGroupFilters)))
            {
                list.Add(getGroupFilterId(f));
            }
            
            return list;
        }

        public void SetProdDrainOn(IMyTerminalBlock drainSorter, IMyTerminalBlock filterSorter)
        {
            if (drainSorter is IMyConveyorSorter & filterSorter is IMyConveyorSorter)
            {
                drainSorter.SetValueBool("DrainAll", false);
                filterSorter.SetValueBool("DrainAll", false);

                CleanFilters(drainSorter as IMyConveyorSorter);
                List<String> list = new List<string>();
                list = getAllGroupFilters();
                list.Remove(getGroupFilterId(SorterGroupFilters.ORE));
                list.Remove(getGroupFilterId(SorterGroupFilters.INGOT));
                AddFilter(drainSorter as IMyConveyorSorter, list, true);

                CleanFilters(filterSorter as IMyConveyorSorter);
                list = new List<string>();
                list.Add(getGroupFilterId(SorterGroupFilters.ORE));
                list.Add(getGroupFilterId(SorterGroupFilters.INGOT));
                AddFilter(filterSorter as IMyConveyorSorter, list, true);

                drainSorter.SetValueBool("DrainAll", true);
            }
        }

        public void SetOreDrainOn(IMyTerminalBlock drainSorter, IMyTerminalBlock filterSorter)
        {
            if (drainSorter is IMyConveyorSorter & filterSorter is IMyConveyorSorter)
            {
                drainSorter.SetValueBool("DrainAll", false);
                filterSorter.SetValueBool("DrainAll", false);

                CleanFilters(drainSorter as IMyConveyorSorter);
                List<String> list = new List<string>();
                list.Add(getGroupFilterId(SorterGroupFilters.ORE));
                AddFilter(drainSorter as IMyConveyorSorter, list, true);

                CleanFilters(filterSorter as IMyConveyorSorter);
                list = getAllGroupFilters();
                list.Remove(getGroupFilterId(SorterGroupFilters.ORE));
                AddFilter(filterSorter as IMyConveyorSorter, list, true);

                drainSorter.SetValueBool("DrainAll", true);
            }
        }

        public void SetDrainOff(IMyTerminalBlock drainSorter, IMyTerminalBlock filterSorter)
        {
            if (drainSorter is IMyConveyorSorter & filterSorter is IMyConveyorSorter)
            {
                drainSorter.SetValueBool("DrainAll", false);
                filterSorter.SetValueBool("DrainAll", false);

                CleanFilters(drainSorter as IMyConveyorSorter);
                List<String> list = new List<string>();
                list = getAllGroupFilters();
                AddFilter(drainSorter as IMyConveyorSorter, list, true);

                CleanFilters(filterSorter as IMyConveyorSorter);
                list = getAllGroupFilters();
                AddFilter(filterSorter as IMyConveyorSorter, list, true);
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            EchoText("!!!DEBUG!!!", false);
            EchoText("Argument: " + argument, true);

            List<ProgramArgument> arguments = ParseArgument(argument);
            List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(blockGroups);

            foreach (ProgramArgument argum in arguments)
            {
                EchoText("Argument: " + argum.getArgument() + ", Int: " + argum.getArgumentInt().ToString(), true);

                if (argum.getArgument().Equals("door"))
                {
                    String groupName = "[Doors #" + argum.getArgumentInt() + "]";
                    EchoText("Action: " + groupName, true);
                    
                    for (int i = 0; i < blockGroups.Count(); i++)
                    {
                        if (blockGroups[i].Name.Equals(groupName))
                        {
                            OpenCloseDoor(blockGroups[i]);
                        }
                    }
                }

                if (argum.getArgument().Equals("cargoInOreOn"))
                {
                    String blockNameIn = "[CargoOreIn #" + argum.getArgumentInt() + "]";
                    String blockNameOut = "[CargoOreOut #" + argum.getArgumentInt() + "]";
                    EchoText("Action: On -> " + blockNameIn + ", " + blockNameOut, true);

                    IMyTerminalBlock inBlock = GridTerminalSystem.GetBlockWithName(blockNameIn);
                    IMyTerminalBlock outBlock = GridTerminalSystem.GetBlockWithName(blockNameOut);

                    if (inBlock != null & outBlock != null)
                    {
                        SetOreDrainOn(inBlock, outBlock);
                    }
                    else
                    {
                        EchoText("Error! Block is not found!", true);
                    }
                }

                if (argum.getArgument().Equals("cargoInOreOff"))
                {
                    String blockNameIn = "[CargoOreIn #" + argum.getArgumentInt() + "]";
                    String blockNameOut = "[CargoOreOut #" + argum.getArgumentInt() + "]";
                    EchoText("Action: Off -> " + blockNameIn + ", " + blockNameOut, true);

                    IMyTerminalBlock inBlock = GridTerminalSystem.GetBlockWithName(blockNameIn);
                    IMyTerminalBlock outBlock = GridTerminalSystem.GetBlockWithName(blockNameOut);

                    if (inBlock != null & outBlock != null)
                    {
                        SetDrainOff(inBlock, outBlock);
                    }
                    else
                    {
                        EchoText("Error! Block is not found!", true);
                    }
                }

                if (argum.getArgument().Equals("cargoInProdOff"))
                {
                    String blockNameIn = "[CargoProdIn #" + argum.getArgumentInt() + "]";
                    String blockNameOut = "[CargoProdOut #" + argum.getArgumentInt() + "]";
                    EchoText("Action: Off -> " + blockNameIn + ", " + blockNameOut, true);

                    IMyTerminalBlock inBlock = GridTerminalSystem.GetBlockWithName(blockNameIn);
                    IMyTerminalBlock outBlock = GridTerminalSystem.GetBlockWithName(blockNameOut);

                    if (inBlock != null & outBlock != null)
                    {
                        SetDrainOff(inBlock, outBlock);
                    }
                    else
                    {
                        EchoText("Error! Block is not found!", true);
                    }
                }

                if (argum.getArgument().Equals("cargoInProdOn"))
                {
                    String blockNameIn = "[CargoProdIn #" + argum.getArgumentInt() + "]";
                    String blockNameOut = "[CargoProdOut #" + argum.getArgumentInt() + "]";
                    EchoText("Action: On -> " + blockNameIn + ", " + blockNameOut, true);

                    IMyTerminalBlock inBlock = GridTerminalSystem.GetBlockWithName(blockNameIn);
                    IMyTerminalBlock outBlock = GridTerminalSystem.GetBlockWithName(blockNameOut);

                    if (inBlock != null & outBlock != null)
                    {
                        SetProdDrainOn(inBlock, outBlock);
                    }
                    else
                    {
                        EchoText("Error! Block is not found!", true);
                    }
                }

                if (argum.getArgument().Equals("refresh"))
                {

                }
            }

            
            
        }
    }
}