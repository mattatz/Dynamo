﻿using System.Collections.ObjectModel;
using Dynamo.Properties;

namespace Dynamo.Configuration
{
    /// <summary>
    /// Group specific style item
    /// Note: This class does not contain special property yet comparing to base class
    /// </summary>
    public class GroupStyleItem: StyleItem
    {
        /// <summary>
        /// Static set of default group styles defined by Dynamo Team
        /// </summary>
        public static ObservableCollection<GroupStyleItem> DefaultGroupStyleItems = 
            new ObservableCollection<GroupStyleItem>() { 
                new GroupStyleItem() { Name = Resources.GroupStyleDefaultActions, HexColorString = Resources.GroupStyleDefaultActionsColor, IsDefault = true },
                new GroupStyleItem() { Name = Resources.GroupStyleDefaultInputs, HexColorString = Resources.GroupStyleDefaultInputsColor, IsDefault = true },
                new GroupStyleItem() { Name = Resources.GroupStyleDefaultOutputs, HexColorString = Resources.GroupStyleDefaultOutputsColor, IsDefault = true },
                new GroupStyleItem() { Name = Resources.GroupStyleDefaultReview, HexColorString = Resources.GroupStyleDefaultReviewColor, IsDefault = true }
            };
    }
}
