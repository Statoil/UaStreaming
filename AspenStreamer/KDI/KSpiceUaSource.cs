﻿using AspenStreamer.Extensions;
using AspenStreamer.KDI;
using Common;
using Common.Contracts;
using Domain.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using UaStreamer.Extensions;
using UaStreamer.Implementation;
using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;
using static Common.Utils;

namespace UaStreamer.KDI
{
    public class KSpiceUaSource : UaSource, IEventSource, ISourceBlock<EventPackage>
    {
        #region Fields
        private static ILogger log = Log.Logger.ForContext<KSpiceUaSource>();

        private BufferBlock<EventPackage> outBuffer;

        private string plantCode;
        private List<KSpiceVariableData> variableInformation;

        public Task Completion => outBuffer.Completion;
        #endregion

        #region Constructor
        public KSpiceUaSource(string plantCode, string serverUrl) : base(ApplicationInstance.Default)
        {
            this.plantCode = plantCode;

            outBuffer = new BufferBlock<EventPackage>(
                    new DataflowBlockOptions() { BoundedCapacity = Constants.KSpiceBufferCapacity});

            Connect(serverUrl);
            MapKSpiceUaServer();

            var itemsToMonitor = variableInformation
                .Select(variable => variable.BuildMonitoredItem(DataChangeTrigger.StatusValueTimestamp, GetUaSamplingInterval()))
                .ToList();

            CreateSubscription();
            AddItemsToSubscription(itemsToMonitor);
        }
        #endregion

        #region interface
        public void StartListening() 
            => EnablePublishing();

        public void StopListening() 
            => DisablePublishing();

        public void Subscribe(IEnumerable<string> tagNames)
        {
            throw new NotImplementedException();
        }

        public void UnSubscribe(IEnumerable<string> tagNames)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Override
        protected override void DataChanged(Subscription subscription, DataChangedEventArgs args)
        {
            foreach (var change in args.DataChanges)
            {
                EventVqt @event = new EventVqt();
                @event.KSpiceFillWith((KSpiceVariableData)change.MonitoredItem.UserData, change);
                var package = new EventPackage(@event, plantCode, Constants.RealTime, new List<string>());

                outBuffer.Post(package);
            }
        }
        #endregion

        #region Private
        private void MapKSpiceUaServer()
        {
            NodeId measurementRoot = new NodeId(Constants.KSpiceRoot, Constants.KSpiceNameSpace);
            variableInformation = BrowseFolderForClass(measurementRoot, NodeClass.Variable)
                .FilterOnTagMatch(GetTagMatchPattern())
                .ExtractKSpiceVariabeInfo(plantCode);
        }
        #endregion

        #region ISourceBlock
        public IDisposable LinkTo(ITargetBlock<EventPackage> target, DataflowLinkOptions linkOptions) 
            => outBuffer.LinkTo(target, linkOptions);

        public EventPackage ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<EventPackage> target, out bool messageConsumed) 
            => ((ISourceBlock<EventPackage>)outBuffer).ConsumeMessage(messageHeader, target, out messageConsumed);

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<EventPackage> target) 
            => ((ISourceBlock<EventPackage>)outBuffer).ReserveMessage(messageHeader, target);

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<EventPackage> target) 
            => ((ISourceBlock<EventPackage>)outBuffer).ReleaseReservation(messageHeader, target);

        public void Complete() 
            => outBuffer.Complete();

        public void Fault(Exception exception) 
            => ((ISourceBlock<EventPackage>)outBuffer).Fault(exception);
        #endregion


    }
}
