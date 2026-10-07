using FortniteBoost.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace FortniteBoost.Services
{
    public class OptimizerService
    {
        public List<OptimizationItem> Optimizations { get; set; } = new();

        public OptimizerService()
        {
            // Perfil Competitivo
            Optimizations.Add(new OptimizationItem
            {
                Name = "Perfil Competitivo",
                Description = "Optimización máxima para Fortnite competitivo",
                ScriptPath = "OptimizeCompetitive.ps1"
            });

            // Perfil Streaming
            Optimizations.Add(new OptimizationItem
            {
                Name = "Perfil Streaming",
                Description = "Optimización balanceada para transmitir",
                ScriptPath = "OptimizeStreaming.ps1"
            });

            // Perfil Default
            Optimizations.Add(new OptimizationItem
            {
                Name = "Perfil Default",
                Description = "Restaurar configuraciones originales",
                ScriptPath = "RestoreDefault.ps1"
            });
        }

        // Nuevo método para obtener la ruta completa del script en bin\...\Scripts
        public string GetScriptFullPath(string scriptName)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", scriptName);
        }

        public void ApplyOptimization(OptimizationItem optimization)
        {
            string fullPath = GetScriptFullPath(optimization.ScriptPath);
            Process.Start("powershell.exe", $"-ExecutionPolicy Bypass -File \"{fullPath}\"");
        }

        public void RestoreOptimization(OptimizationItem optimization)
        {
            string fullPath = GetScriptFullPath(optimization.ScriptPath);
            Process.Start("powershell.exe", $"-ExecutionPolicy Bypass -File \"{fullPath}\"");
        }
    }
}
