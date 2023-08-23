using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Trace.Model;

internal interface ITimeSpanned
{
    public TimeSpan StartTime { get; }
    public TimeSpan EndTime { get; }
    public TimeSpan TimeSpan { get; }
}
