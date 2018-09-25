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
        // DEBUG LCD = "[DEBUG]"
        // Horizontal rotor prefix = "[SS-ROTOR-HOR]"
        // Vertical rotor prefix = "[SS-ROTOR-VERT]"

        public Program()
        {
            // It's recommended to set RuntimeInfo.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
        }

        public void Save()
        {
        }

        MyCommandLine commandLine = new MyCommandLine();

        IMyMotorStator horizontalRotor;
        IMyMotorStator verticalRotor;

        float rotorVelocity = 1f; //default
        float defDeltaAngle = 10f;

        void EchoText(String text, Boolean append)
        {
            try
            {
                IMyTextPanel lcd = GridTerminalSystem.GetBlockWithName("[DEBUG]") as IMyTextPanel;
                lcd.WritePublicText(text + System.Environment.NewLine, append);
            }
            catch (Exception e) { }
        }

        void SetAnlge(IMyMotorStator rotor, float angle)
        {
            if (rotor != null) {
                float currentAngle = rotor.Angle / (float)Math.PI * 180f;
                EchoText("Move rotor from the current angle["+currentAngle+"] to new Angle["+angle+"]", true);
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
                //null
            }
        }

        int FindBetterPossition4Rotor(IMyMotorStator rotor, List<IMySolarPanel> solarPanels)
        {
            EchoText("Rotor[" + rotor.Name + "] is in progress", true);
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
            try
            {
                EchoText("---------- Solar System ----------", true);
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
                EchoText("Solar panels: " + solarPanels.Count, true);

                List<IMyMotorStator> allRotors = null;
                GridTerminalSystem.GetBlocksOfType<IMyMotorStator>(allRotors);

                foreach (IMyMotorStator rotor in allRotors)
                {
                    if (rotor.Name.Contains("[SS-ROTOR-HOR]"))
                    {
                        horizontalRotor = rotor;
                        EchoText("Horizontal Rotor: " + rotor.Name, true);
                    }
                    if (rotor.Name.Contains("[SS-ROTOR-VERT]"))
                    {
                        verticalRotor = rotor;
                        EchoText("Vertical Rotor: " + rotor.Name, true);
                    }
                }

                if (solarPanels != null & horizontalRotor != null & verticalRotor != null)
                {
                    EchoText("-------- Looking for new possition --------", true);
                    FindBetterPossition(solarPanels);
                }
            }catch(Exception e)
            {
                EchoText(e.Message, true);
            }
        }
    }
}