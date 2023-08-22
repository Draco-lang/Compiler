using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Trace.Model;

internal interface ITimeSpanned
{
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    public TimeSpan TimeSpan { get; }
}
