﻿using System;
using System.Collections;
using System.Linq;
using Comfort.Common;
using EFT.Interactive;
using EFT;
using HarmonyLib;

namespace SPTOpenSesame.Helpers
{
    public static class InteractionHelpers
    {
        public static Type TargetType { get; private set; } = null;

        private static Type resultType = null;
        private static Type actionType = null;

        public static void LoadTypes()
        {
            Type[] targetTypeOptions = Aki.Reflection.Utils.PatchConstants.EftTypes.Where(t => t.GetMethods().Any(m => m.Name.Contains("GetAvailableActions"))).ToArray();
            if (targetTypeOptions.Length != 1)
            {
                throw new TypeLoadException("Cannot find target method");
            }

            TargetType = targetTypeOptions[0];
            LoggingUtil.LogInfo("Target type: " + TargetType.FullName);

            resultType = AccessTools.FirstMethod(TargetType, m => m.Name.Contains("GetAvailableActions")).ReturnType;
            LoggingUtil.LogInfo("Return type: " + resultType.FullName);

            actionType = AccessTools.Field(resultType, "SelectedAction").FieldType;
            LoggingUtil.LogInfo("Action type: " + actionType.FullName);
        }

        public static bool HaveTypesBeenLoaded()
        {
            if ((TargetType == null) || (resultType == null) || (actionType == null))
            {
                return false;
            }

            return true;
        }

        public static bool isInteractorABot(GamePlayerOwner owner)
        {
            if (owner?.Player?.Id != Singleton<GameWorld>.Instance?.MainPlayer?.Id)
            {
                return true;
            }

            return false;
        }

        public static bool canToggle(this WorldInteractiveObject interactiveObject)
        {
            if (!interactiveObject.Operatable)
            {
                return false;
            }

            if (interactiveObject.DoorState != EDoorState.Shut)
            {
                return false;
            }

            return true;
        }

        public static void addDoNothingToActionList(object actionListObject)
        {
            if (!OpenSesamePlugin.AddDoNothingAction.Value)
            {
                return;
            }

            if (!HaveTypesBeenLoaded())
            {
                throw new TypeLoadException("Types have not been loaded");
            }

            // Create a new action to do nothing
            var newAction = Activator.CreateInstance(actionType);

            AccessTools.Field(actionType, "Name").SetValue(newAction, "DoNothing");

            InteractiveObjectInteractionWrapper unlockActionWrapper = new InteractiveObjectInteractionWrapper();
            AccessTools.Field(actionType, "Action")
                .SetValue(newAction, new Action(unlockActionWrapper.doNothingAction));

            AccessTools.Field(actionType, "Disabled").SetValue(newAction, false);

            // Add the new action to the context menu for the door
            IList actionList =
                (IList)AccessTools.Field(resultType, "Actions").GetValue(actionListObject);
            actionList.Add(newAction);
        }

        public static void addOpenSesameToActionList(this WorldInteractiveObject interactiveObject, object actionListObject, GamePlayerOwner owner)
        {
            // Don't do anything else unless the door is locked and requires a key
            if ((interactiveObject.DoorState != EDoorState.Locked) || (interactiveObject.KeyId == ""))
            {
                return;
            }

            if (!HaveTypesBeenLoaded())
            {
                throw new TypeLoadException("Types have not been loaded");
            }

            // Add "Do Nothing" to the action list as the default selection
            addDoNothingToActionList(actionListObject);

            // Create a new action to unlock the door
            var newAction = Activator.CreateInstance(actionType);

            AccessTools.Field(actionType, "Name").SetValue(newAction, "OpenSesame");

            InteractiveObjectInteractionWrapper unlockActionWrapper =
                new InteractiveObjectInteractionWrapper(interactiveObject, owner);
            AccessTools.Field(actionType, "Action")
                .SetValue(newAction, new Action(unlockActionWrapper.unlockAndOpenAction));

            AccessTools.Field(actionType, "Disabled")
                .SetValue(newAction, !interactiveObject.Operatable);

            // Add the new action to the context menu for the door
            IList actionList =
                (IList)AccessTools.Field(resultType, "Actions").GetValue(actionListObject);
            actionList.Add(newAction);
        }

        public static void addTurnOnPowerToActionList(this WorldInteractiveObject interactiveObject, object actionListObject)
        {
            if (!HaveTypesBeenLoaded())
            {
                throw new TypeLoadException("Types have not been loaded");
            }

            // Add "Do Nothing" to the action list as the default selection
            addDoNothingToActionList(actionListObject);

            // Create a new action to turn on the power switch
            var newAction = Activator.CreateInstance(actionType);

            AccessTools.Field(actionType, "Name").SetValue(newAction, "TurnOnPower");

            InteractiveObjectInteractionWrapper turnOnPowerActionWrapper =
                new InteractiveObjectInteractionWrapper(OpenSesamePlugin.PowerSwitch);
            AccessTools.Field(actionType, "Action")
                .SetValue(newAction, new Action(turnOnPowerActionWrapper.turnOnAction));

            AccessTools.Field(actionType, "Disabled")
                .SetValue(newAction, !OpenSesamePlugin.PowerSwitch.canToggle());

            // Add the new action to the context menu for the door
            IList actionList =
                (IList)AccessTools.Field(resultType, "Actions").GetValue(actionListObject);
            actionList.Add(newAction);
        }

        internal sealed class InteractiveObjectInteractionWrapper
        {
            public GamePlayerOwner owner;
            public WorldInteractiveObject interactiveObject;

            public InteractiveObjectInteractionWrapper()
            {
            }

            public InteractiveObjectInteractionWrapper(WorldInteractiveObject _interactiveObject) : this()
            {
                interactiveObject = _interactiveObject;
            }

            public InteractiveObjectInteractionWrapper(WorldInteractiveObject _interactiveObject,
                GamePlayerOwner _owner) : this(_interactiveObject)
            {
                owner = _owner;
            }

            internal void doNothingAction()
            {
                LoggingUtil.LogInfo("Nothing happened. What did you expect...?");
            }

            internal void unlockAndOpenAction()
            {
                if (interactiveObject == null)
                {
                    LoggingUtil.LogError("Cannot unlock and open a null object");
                    return;
                }

                if (owner == null)
                {
                    LoggingUtil.LogError("A GamePlayerOwner must be defined to unlock and open object " +
                                         interactiveObject.Id);
                    return;
                }

                if (OpenSesamePlugin.WriteMessagesWhenUnlockingDoors.Value)
                {
                    LoggingUtil.LogInfo("Unlocking interactive object " + interactiveObject.Id +
                                        " which requires key " + interactiveObject.KeyId + "...");
                }

                // Unlock the door
                interactiveObject.DoorState = EDoorState.Shut;
                interactiveObject.OnEnable();

                // Do not open lootable containers like safes, cash registers, etc.
                if ((interactiveObject as LootableContainer) != null)
                {
                    return;
                }

                if (OpenSesamePlugin.WriteMessagesWhenUnlockingDoors.Value)
                {
                    LoggingUtil.LogInfo("Opening interactive object " + interactiveObject.Id + "...");
                }

                owner.Player.MovementContext.ResetCanUsePropState();

                // Open the door
                var gstruct = Door.Interact(this.owner.Player, EInteractionType.Open);
                if (!gstruct.Succeeded)
                {
                    return;
                }

                owner.Player.CurrentManagedState.ExecuteDoorInteraction(interactiveObject, gstruct.Value, null,
                    owner.Player);
            }

            internal void turnOnAction()
            {
                if (interactiveObject == null)
                {
                    LoggingUtil.LogError("Cannot toggle a null switch");
                    return;
                }

                if (!interactiveObject.canToggle())
                {
                    LoggingUtil.LogWarning("Cannot interact with object " + interactiveObject.Id + " right now");
                    return;
                }

                if (OpenSesamePlugin.WriteMessagesWhenTogglingSwitches.Value)
                {
                    LoggingUtil.LogInfo("Toggling object " + interactiveObject.Id + "...");
                }

                Player you = Singleton<GameWorld>.Instance.MainPlayer;
                you.CurrentManagedState.ExecuteDoorInteraction(interactiveObject,
                    new InteractionResult(EInteractionType.Open), null, you);
            }
        }
    }
}