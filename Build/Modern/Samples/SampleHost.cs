using DigitalRune.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

// A small host for unchanged upstream sample classes. Scene setup and math come from those files.
namespace Samples
{
    public enum SampleCategory { Mathematics, Physics }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SampleAttribute(SampleCategory category, string description, string details, int order) : Attribute { }
    public class BasicSample
    {
        public SampleScreen GraphicsScreen { get; } = new();
        public BasicSample(Game game) { }
    }
    public sealed class SampleScreen
    {
        public bool ClearBackground { get; set; }
        public TextOutput DebugRenderer2D { get; } = new();
    }
    public sealed class TextOutput { public void DrawText(string text) => Console.WriteLine(text); }
    public class PhysicsSample
    {
        public Simulation Simulation { get; } = new();
        public PhysicsSample(Game game) { }
    }
}

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        bool math=args.Contains("math"),headless=args.Contains("--headless");
        if(math) { _=new Samples.Mathematics.BasicMathSample(null); return 0; }
        if(headless)
        {
            var sample=new Samples.Physics.BoxStackSample(null);
            for(int i=0;i<600;i++)sample.Simulation.Update(1f/60);
            var boxes=sample.Simulation.RigidBodies.Where(body=>body.Name?.StartsWith("Box")==true).ToArray();
            if(boxes.Length!=5 || boxes.Any(body=>!float.IsFinite(body.Pose.Position.Y)))throw new Exception("Invalid built-in box stack result");
            Console.WriteLine("PASS built-in BoxStackSample: 5 boxes, 600 physics steps, finite poses");return 0;
        }
        using var game=new BoxStackHost(args.Contains("--smoke"));game.Run();return 0;
    }
}

internal sealed class BoxStackHost : Game
{
    readonly GraphicsDeviceManager graphics;
    readonly bool smoke;
    Samples.Physics.BoxStackSample sample;
    BasicEffect effect;
    int frames;
    public BoxStackHost(bool smoke)
    {
        this.smoke=smoke;graphics=new GraphicsDeviceManager(this) { PreferredBackBufferWidth=1000,PreferredBackBufferHeight=700 };
        Window.Title="DigitalRune built-in BoxStackSample";IsMouseVisible=true;
    }
    protected override void LoadContent()
    {
        sample=new Samples.Physics.BoxStackSample(this);
        effect=new BasicEffect(GraphicsDevice) { VertexColorEnabled=true,View=Matrix.CreateLookAt(new Vector3(8,6,10),new Vector3(0,2,0),Vector3.Up),
            Projection=Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,GraphicsDevice.Viewport.AspectRatio,.1f,100f) };
        Console.WriteLine("Running upstream Physics/01-BoxStackSample.cs. Escape closes the sample.");
    }
    protected override void Update(GameTime time)
    {
        if(Keyboard.GetState().IsKeyDown(Keys.Escape))Exit();
        sample.Simulation.Update((float)time.ElapsedGameTime.TotalSeconds);
        if(smoke && ++frames>=30){Console.WriteLine("PASS built-in sample graphical smoke: 30 frames");Exit();}
        base.Update(time);
    }
    protected override void Draw(GameTime time)
    {
        GraphicsDevice.Clear(new Color(17,24,39));GraphicsDevice.DepthStencilState=DepthStencilState.Default;
        var lines=new List<VertexPositionColor>();
        void Line(Vector3 a,Vector3 b,Color color){lines.Add(new(a,color));lines.Add(new(b,color));}
        for(int i=-7;i<=7;i++){Line(new(i,0,-7),new(i,0,7),Color.DimGray);Line(new(-7,0,i),new(7,0,i),Color.DimGray);}
        foreach(var body in sample.Simulation.RigidBodies.Where(b=>b.Name?.StartsWith("Box")==true))
        {
            var p=body.Pose;var o=p.Orientation;
            var world=new Matrix(o.M00,o.M10,o.M20,0,o.M01,o.M11,o.M21,0,o.M02,o.M12,o.M22,0,p.Position.X,p.Position.Y,p.Position.Z,1);
            var corners=Enumerable.Range(0,8).Select(i=>Vector3.Transform(new Vector3((i&1)==0?-.4f:.4f,(i&2)==0?-.4f:.4f,(i&4)==0?-.4f:.4f),world)).ToArray();
            for(int i=0;i<8;i++)for(int bit=1;bit<=4;bit*=2)if((i&bit)==0)Line(corners[i],corners[i|bit],Color.CornflowerBlue);
        }
        foreach(var pass in effect.CurrentTechnique.Passes){pass.Apply();GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList,lines.ToArray(),0,lines.Count/2);}
        base.Draw(time);
    }
    protected override void UnloadContent(){effect?.Dispose();base.UnloadContent();}
}
