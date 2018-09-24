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
using VRage.Game.ModAPI.Ingame.Utilities;
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

        MyCommandLine commandLine = new MyCommandLine();

        IMyMotorStator horizontalRotor;
        IMyMotorStator verticalRotor;

        float rotorVelocity = 1f; //default
        float defDeltaAngle = 10f;

        MyIni parser = new MyIni();

        void SetAnlge(IMyMotorStator rotor, float angle)
        {
            if (rotor != null) {
                float currentAngle = rotor.Angle / (float)Math.PI * 180f;
                if (currentAngle != angle)
                {
                    if(angle > currentAngle)
                    {
                        rotor.SetValue<float>("UpperLimit", angle);
                        rotor.SetValue<float>("LowerLimit", -361f);
                        rotor.SetValue<float>("Velocity", rotorVelocity);
                    }
                    else
                    {
                        rotor.SetValue<float>("LowerLimit", angle);
                        rotor.SetValue<float>("UpperLimit", 361f);
                        rotor.SetValue<float>("Velocity", -rotorVelocity);
                    }
                }
            }
        }

        void LiftRotor(IMyMotorStator rotor, float deltaAnlge)
        {
            SetAnlge(rotor, (rotor.Angle / (float)Math.PI * 180f) + deltaAnlge);
        }

        float GetCurrentOutput(List<IMySolarPanel> solarPanels)
        {
            float currentOutput = 0f;
            foreach (IMySolarPanel panel in solarPanels)
            {
                currentOutput = currentOutput + panel.CurrentOutput;
            }
            return currentOutput;
        }

        bool CheckIsRotorsMoving()
        {
            if ((horizontalRotor.Angle == horizontalRotor.LowerLimitRad || horizontalRotor.Angle == horizontalRotor.UpperLimitRad) &
                (verticalRotor.Angle == verticalRotor.LowerLimitRad || verticalRotor.Angle == verticalRotor.UpperLimitRad)){
                return true;
            }else{
                return false;
            }
        }

        void WaitTillRotorsMove()
        {
            while (!CheckIsRotorsMoving())
            {
                //null;
            }
        }

        int FindBetterPossition4Rotor(IMyMotorStator rotor, List<IMySolarPanel> solarPanels)
        {
            WaitTillRotorsMove();
            float initialOutput = GetCurrentOutput(solarPanels);
            LiftRotor(rotor, defDeltaAngle);
            int result = 1;
            WaitTillRotorsMove();
            if (initialOutput > GetCurrentOutput(solarPanels))
            {
                //move opposite side
                LiftRotor(rotor, -2*defDeltaAngle);
            }
            WaitTillRotorsMove();
            if (initialOutput > GetCurrentOutput(solarPanels))
            {
                //restore possition
                LiftRotor(rotor, defDeltaAngle);
                result = 0; // no need to move. relax and enjoy the sun :)
            }
            return result;
        }

        void FindBetterPossition(List<IMySolarPanel> solarPanels)
        {
            int procInd = 1;
            while (procInd != 0)
            {
                procInd = FindBetterPossition4Rotor(verticalRotor, solarPanels);
                procInd = procInd + FindBetterPossition4Rotor(horizontalRotor, solarPanels);
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (commandLine.TryParse(argument))
            {
                //get attribute
                string arg = commandLine.Argument(0);
                if (arg == null) { }

                //check switch
                if (commandLine.Switch("taste my d")) { }
            }

            List<IMySolarPanel> solarPanels = null;
            GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(solarPanels);
            List<IMyMotorStator> allRotors = null;
            GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(allRotors);
            
            foreach(IMyMotorStator rotor in allRotors)
            {
                if (rotor.Name.Contains("[SS-ROTOR-HOR]"))
                {
                    horizontalRotor = rotor;
                }
                if (rotor.Name.Contains("[SS-ROTOR-VERT]"))
                {
                    verticalRotor = rotor;
                }
            }

            if (solarPanels != null & horizontalRotor != null & verticalRotor != null)
            {
                FindBetterPossition(solarPanels);
            }


            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.
        }
    }
}