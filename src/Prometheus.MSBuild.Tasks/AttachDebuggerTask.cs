using System;
using BuildUtil = Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using System.IO;
using System.Reflection;
using System.Security;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Prometheus.MSBuild.Tasks.Extension;

namespace Prometheus.MSBuild.Tasks
{
    public class AttachDebuggerTask : BuildUtil.Task
    {
        public override bool Execute()
        {
            if (!Debugger.IsAttached)
                Debugger.Launch();
            else
                Debugger.Break();

            return true;
        }
    }
}
