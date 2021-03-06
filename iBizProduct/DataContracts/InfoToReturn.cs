﻿// Copyright (c) iBizVision - 2014
// Author: Dan Siegel

using System.ComponentModel;

namespace iBizProduct.DataContracts
{
    /// <summary>
    /// Base InfoToReturn Object
    /// </summary>
    public abstract class InfoToReturn
    {
        /// <summary>
        /// By default the InfoToReturn is set to return all. If you want to limit how much 
        /// data is returned you should set this to false.
        /// </summary>
        [DefaultValue( true )]
        public bool ALL { get; set; }

    }
}
