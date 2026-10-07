using System;
using System.Collections.Generic;
using System.Text;

namespace FortniteBoost.Models
{
    public class OptimizationItem
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ScriptPath { get; set; } = string.Empty;
        public bool IsApplied { get; set; } = false;
    }
}
