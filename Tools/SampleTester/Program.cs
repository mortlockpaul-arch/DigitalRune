using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;

internal static class Tester
{
    [STAThread] static void Main() => new Application().Run(new TesterWindow());
}

internal sealed class TesterWindow : Window
{
    readonly TextBox repo=new() { Text=@"C:\source\Indie-Games-Core\DigitalRune" };
    readonly ComboBox backend=new() { ItemsSource=new[]{"FNA","MonoGame"},SelectedIndex=0,MinWidth=150 };
    readonly ComboBox sample=new() { ItemsSource=new[]{"Physics · BoxStackSample","Mathematics · BasicMathSample"},SelectedIndex=0,MinWidth=300 };
    readonly CheckBox headless=new() { Content="Headless check (no graphics window)",Margin=new(6,10,6,10) };
    readonly Button run=new() { Content="Run built-in sample",MinWidth=170 };
    readonly Button stop=new() { Content="Stop",IsEnabled=false };
    readonly TextBox output=new() { IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,FontFamily=new("Consolas") };
    Process? process;
    public TesterWindow()
    {
        Title="DigitalRune Sample Tester";Width=1000;Height=720;MinWidth=760;MinHeight=560;FontSize=14;FontFamily=new("Segoe UI");
        Background=(Brush)new BrushConverter().ConvertFromString("#111827")!;Foreground=Brushes.White;
        Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Parse("""
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <Style TargetType="TextBlock"><Setter Property="Foreground" Value="#E5E7EB"/><Setter Property="Margin" Value="6"/></Style>
          <Style TargetType="Button"><Setter Property="Margin" Value="6"/><Setter Property="Padding" Value="14,9"/><Setter Property="Foreground" Value="White"/><Setter Property="Background" Value="#374151"/><Setter Property="Template"><Setter.Value><ControlTemplate TargetType="Button"><Border x:Name="B" Background="{TemplateBinding Background}" CornerRadius="8" Padding="{TemplateBinding Padding}"><ContentPresenter HorizontalAlignment="Center"/></Border><ControlTemplate.Triggers><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="B" Property="Background" Value="#536176"/></Trigger><Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.5"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
          <Style TargetType="TextBox"><Setter Property="Margin" Value="6"/><Setter Property="Padding" Value="9"/><Setter Property="Background" Value="#1F2937"/><Setter Property="Foreground" Value="White"/><Setter Property="BorderBrush" Value="#4B5563"/></Style>
          <Style TargetType="ComboBox"><Setter Property="Margin" Value="6"/><Setter Property="Padding" Value="8"/></Style>
          <Style TargetType="CheckBox"><Setter Property="Foreground" Value="White"/></Style>
        </ResourceDictionary>
        """));
        var root=new DockPanel { Margin=new(18) };Content=root;
        var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
        top.Children.Add(new TextBlock { Text="DigitalRune built-in samples",FontSize=25,FontWeight=FontWeights.SemiBold });
        top.Children.Add(new TextBlock { Text="Runs unchanged sample classes from your fork using a small runtime host. Full engine/sample-browser content is not required.",TextWrapping=TextWrapping.Wrap });
        top.Children.Add(new TextBlock { Text="DigitalRune repository" });top.Children.Add(repo);
        var options=new WrapPanel();options.Children.Add(backend);options.Children.Add(sample);top.Children.Add(options);top.Children.Add(headless);
        var actions=new WrapPanel();run.Background=(Brush)new BrushConverter().ConvertFromString("#2563EB")!;
        actions.Children.Add(run);actions.Children.Add(stop);
        var source=new Button { Content="Open sample source" };source.Click+=(_,_)=>OpenSource();actions.Children.Add(source);top.Children.Add(actions);
        top.Children.Add(new TextBlock { Text="BoxStack: Escape closes the graphics window. Mathematics writes its results below. First run restores and builds the selected backend.",TextWrapping=TextWrapping.Wrap });
        root.Children.Add(output);run.Click+=async(_,_)=>await Run();stop.Click+=(_,_)=>Stop();Closed+=(_,_)=>Stop();
    }
    void Log(string text)=>Dispatcher.BeginInvoke(()=>{output.AppendText(text+Environment.NewLine);output.ScrollToEnd();});
    string SampleFile=>Path.Combine(repo.Text,"Samples","Samples","Samples",sample.SelectedIndex==0?"Physics":"Mathematics",sample.SelectedIndex==0?"01-BoxStackSample.cs":"01-BasicMathSample.cs");
    void OpenSource()
    {
        if(File.Exists(SampleFile))Process.Start(new ProcessStartInfo(SampleFile){UseShellExecute=true});
        else Log("Sample source not found. Check the repository path.");
    }
    async Task Run()
    {
        if(process is { HasExited:false })return;
        string project=Path.Combine(repo.Text,"Build","Modern","Samples","Samples.csproj");
        if(!File.Exists(project)){Log("Modern sample host not found. Check the repository path.");return;}
        run.IsEnabled=false;stop.IsEnabled=true;repo.IsEnabled=backend.IsEnabled=sample.IsEnabled=headless.IsEnabled=false;
        try
        {
            output.Clear();Log("Building/running "+backend.SelectedItem+" · "+sample.SelectedItem);
            int build=await Execute("build",project,"-m:1","--disable-build-servers","-p:GraphicsBackend="+backend.SelectedItem,"-p:Platform=x64","-p:PlatformTarget=x64","-p:NuGetAudit=false");
            if(build!=0){Log("Build failed with code "+build);return;}
            string dll=Path.Combine(repo.Text,"Build","Modern","Samples","bin",backend.SelectedItem.ToString()!,"x64","Debug","net9.0-windows","Samples.dll");
            var arguments=new List<string>{dll,sample.SelectedIndex==0?"boxes":"math"};if(headless.IsChecked==true)arguments.Add("--headless");
            Log("Build complete. Starting the built-in sample…");int exit=await Execute(arguments.ToArray());Log("Exited with code "+exit);
        }
        catch(Exception ex){Log(ex.Message);}
        finally {process?.Dispose();process=null;run.IsEnabled=true;stop.IsEnabled=false;repo.IsEnabled=backend.IsEnabled=sample.IsEnabled=headless.IsEnabled=true;}
    }
    async Task<int> Execute(params string[] arguments)
    {
        var info=new ProcessStartInfo("dotnet") { WorkingDirectory=repo.Text,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
        foreach(string argument in arguments)info.ArgumentList.Add(argument);
        info.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"]="1";info.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"]="0";
        // A tester launched by dotnet run/Visual Studio must not force its own SDK into child builds.
        foreach(string name in new[]{"MSBUILD_EXE_PATH","MSBuildSDKsPath","MSBuildExtensionsPath","DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR","DOTNET_MSBUILD_SDK_RESOLVER_SDKS_VER","DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR"})info.Environment.Remove(name);
        process=new Process { StartInfo=info };
        process.OutputDataReceived+=(_,e)=>{if(e.Data!=null)Log(e.Data);};process.ErrorDataReceived+=(_,e)=>{if(e.Data!=null)Log(e.Data);};
        process.Start();process.BeginOutputReadLine();process.BeginErrorReadLine();await process.WaitForExitAsync();process.WaitForExit();
        int result=process.ExitCode;process.Dispose();process=null;await Dispatcher.InvokeAsync(()=>{},System.Windows.Threading.DispatcherPriority.Background);return result;
    }
    void Stop(){try{if(process is {HasExited:false})process.Kill(entireProcessTree:true);}catch(InvalidOperationException){} }
}
