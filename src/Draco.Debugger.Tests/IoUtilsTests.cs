using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Debugger.IO;
using Xunit;

namespace Draco.Debugger.Tests;
public class IoUtilsTests
{
    [Fact]
    public async Task CapturedIOReadsReturnsZero()
    {
        var result = IoUtils.CaptureProcess(() =>
            Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet"
            }), out var handles);
        var reader = new StreamReader(handles.StandardOutputReader);
        var buffer = new char[1024];
        var amount = await reader.ReadAsync(buffer);
        Assert.True(amount > 0);
        var str = new string(buffer, 0, amount);
        var emptyRead = await reader.ReadAsync(buffer);
        Assert.Equal(0, emptyRead);
    }
}
