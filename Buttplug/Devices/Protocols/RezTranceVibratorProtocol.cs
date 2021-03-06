﻿// <copyright file="RezTranceVibratorDevice.cs" company="Nonpolynomial Labs LLC">
// Buttplug C# Source Code File - Visit https://buttplug.io for more info about the project.
// Copyright (c) Nonpolynomial Labs LLC. All rights reserved.
// Licensed under the BSD 3-Clause license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Buttplug.Core;
using Buttplug.Core.Logging;
using Buttplug.Core.Messages;
using Buttplug.Devices;

namespace Buttplug.Server.Managers.WinUSBManager
{
    internal class RezTranceVibratorProtocol : ButtplugDeviceProtocol
    {
        private static uint _vibratorCount = 1;

        public RezTranceVibratorProtocol(IButtplugLogManager aLogManager, ButtplugDeviceImpl aDevice)
            : base(aLogManager, "Trancevibrator", aDevice)
        {
            AddMessageHandler<SingleMotorVibrateCmd>(HandleSingleMotorVibrateCmd);
            AddMessageHandler<StopDeviceCmd>(HandleStopDeviceCmd);
            AddMessageHandler<VibrateCmd>(HandleVibrateCmd, new MessageAttributes { FeatureCount = 1 });
        }

        private Task<ButtplugMessage> HandleStopDeviceCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            BpLogger.Debug("Stopping Device " + Name);
            return HandleSingleMotorVibrateCmd(new SingleMotorVibrateCmd(aMsg.DeviceIndex, 0, aMsg.Id), aToken);
        }

        private async Task<ButtplugMessage> HandleVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<VibrateCmd>(aMsg);

            if (cmdMsg.Speeds.Count == 0 || cmdMsg.Speeds.Count > _vibratorCount)
            {
                throw new ButtplugDeviceException(BpLogger, "VibrateCmd requires 1 speed for this device.",
                    cmdMsg.Id);
            }

            foreach (var v in cmdMsg.Speeds)
            {
                if (v.Index >= _vibratorCount)
                {
                    throw new ButtplugDeviceException(BpLogger,
                        $"Index {v.Index} is out of bounds for VibrateCmd for this device.",
                        cmdMsg.Id);
                }

                await Interface.WriteValueAsync(new[] { (byte)Math.Floor(v.Speed * 255) },
                    new ButtplugDeviceWriteOptions { Endpoint = Endpoints.TxVendorControl },
                    aToken).ConfigureAwait(false);
            }

            return new Ok(aMsg.Id);
        }

        private Task<ButtplugMessage> HandleSingleMotorVibrateCmd(ButtplugDeviceMessage aMsg, CancellationToken aToken)
        {
            var cmdMsg = CheckMessageHandler<SingleMotorVibrateCmd>(aMsg);

            var speeds = new List<VibrateCmd.VibrateSubcommand>();
            for (uint i = 0; i < _vibratorCount; i++)
            {
                speeds.Add(new VibrateCmd.VibrateSubcommand(i, cmdMsg.Speed));
            }

            return HandleVibrateCmd(new VibrateCmd(aMsg.DeviceIndex, speeds, aMsg.Id), aToken);
        }
    }
}
