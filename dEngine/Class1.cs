using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using Microsoft.DirectX.AudioVideoPlayback;
using Microsoft.DirectX.DirectSound;
using System.IO;
using System.Net;

namespace dEngine
{
    public class Main
    {
        public static Microsoft.DirectX.Direct3D.Device device;
        public static Microsoft.DirectX.DirectInput.Device kbdevice;
        public static Microsoft.DirectX.DirectInput.Device msdevice;
        public static Microsoft.DirectX.DirectSound.Device sdevice;
        private KeyboardState kbState;
        private Texture PrimitiveTexture;
        private List<Vector3> PrimitiveVertex = new List<Vector3>();
        private List<Vector2> PrimitiveTexs = new List<Vector2>();
        private List<Color> PrimitiveVertexColor = new List<Color>();
        private bool Camera = false;
        public bool CamTask = false;
        private Vector3[] CameraAt = new Vector3[100], CameraLook = new Vector3[100], CameraUp = new Vector3[100];
        public static Vector3 CamAt, CamLook, CamUp;
        private int CameraOn = 0;
        private Vector3 WorldRotation = new Vector3(0, 0, 0), WorldTranslation = new Vector3(0, 0, 0);
        private Vector4 WorldAxis = new Vector4(0, 0, 0, 0);
        private bool Transform = false;
        private Matrix NormalMat;
        private List<CollideObj> Collides = new List<CollideObj>();
        private List<string> CollideName = new List<string>();
        private List<int> FakeCollides = new List<int>();
        private bool Collide = true, NormalCollide;
        private float CollidePerPoint = 0.01f;
        private Microsoft.DirectX.Direct3D.Font Font;
        private System.Drawing.Font OFont;
        Form Wnd;
        private bool Lighting = false;
        private Cull Culling = new Cull();
        public static string TextureFolder = "", ModelFolder = "", SoundFolder = "";
        private float NearRender = 1f, FarRender = 300f;
        private Vector2 MouseOld = new Vector2(0f, 0f);
        private Color AmbientColor = Color.White;
        private Vector3 TransformPos = new Vector3(0f, 0f, 0f);
        private Key LastKBKey;
        private Vector3 VideoRenderPos, VideoRenderTo;
        private float Gravity = 0;
        private bool Physics = false;
        private List<Obj> Objects = new List<Obj>();
        private List<Timers> Timer = new List<Timers>();
        private int Frames, LastTickCount;
        private float LastFrameRate;
        public Microsoft.DirectX.Direct3D.Effect effect;
        private int Lights = 0;
        private bool ConnectWorld = false;
        private List<TerrainObj> Terrains = new List<TerrainObj>();
        //Settings
        public int VertexProcess = 401;
        public bool Fullscreen = false;
        public Vector2 FullscreenSize = new Vector2(640, 480);
        public TextureFilter Filtering = TextureFilter.None;
        public int MipCount = 0;
        //----
        private Material Material = new Material();
        private List<CameraTsk> CameraTasks = new List<CameraTsk>();
        private int CameraTasksAt = 0;
        private List<EffectObj> Effect = new List<EffectObj>();
        private int SecondFrame = 0;
        public Random Random = new Random();
        private Texture renderTexture = null;
        private Surface renderSurface = null;
        private RenderToSurface renderToSurface = null;
        private Graphics WndG;
        public int Triangles = 0, Points = 0;
        public Microsoft.DirectX.Direct3D.Device Initialize(Form Window)
        {
            Wnd = Window;
            PresentParameters presentParams = new PresentParameters();
            presentParams.Windowed = !Fullscreen;
            if (Fullscreen)
            {
                presentParams.BackBufferCount = 1;
                presentParams.BackBufferWidth = (int)FullscreenSize.X;
                presentParams.BackBufferHeight = (int)FullscreenSize.Y;
                presentParams.BackBufferFormat = Format.A8R8G8B8;
                presentParams.DeviceWindow = Window;
                presentParams.MultiSample = MultiSampleType.TwoSamples;
                presentParams.MultiSampleQuality = 0;
            }
            presentParams.SwapEffect = SwapEffect.Copy;
            presentParams.EnableAutoDepthStencil = true;
            presentParams.AutoDepthStencilFormat = DepthFormat.D16;
            //Window.Size = new Size((int)FullscreenSize.X, (int)FullscreenSize.Y);
            WndG = Wnd.CreateGraphics();

            Culling = Cull.CounterClockwise;

            CreateFlags cflags = CreateFlags.HardwareVertexProcessing;
            if (VertexProcess == Variable.VERTEXPROCESS_SOFTWARE) { cflags = CreateFlags.SoftwareVertexProcessing; }
            return new Microsoft.DirectX.Direct3D.Device(0, Microsoft.DirectX.Direct3D.DeviceType.Hardware, Window, cflags, presentParams);
        }
        public Microsoft.DirectX.DirectInput.Device InputInit(Form Window)
        {
            Microsoft.DirectX.DirectInput.Device Dev = new Microsoft.DirectX.DirectInput.Device(SystemGuid.Keyboard);
            Dev.SetCooperativeLevel(Window, CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);
            Dev.Acquire();
            return Dev;
        }
        public Microsoft.DirectX.DirectInput.Device MouseInit(Form Window)
        {
            Microsoft.DirectX.DirectInput.Device Dev = new Microsoft.DirectX.DirectInput.Device(SystemGuid.Mouse);
            Dev.SetCooperativeLevel(Window, CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);
            Dev.Acquire();
            return Dev;
        }
        public Microsoft.DirectX.DirectSound.Device SoundInit(Form Window)
        {
            sdevice = new Microsoft.DirectX.DirectSound.Device();
            sdevice.SetCooperativeLevel(Window, CooperativeLevel.Normal);
            BufferDescription desc = new BufferDescription();
            desc.ControlEffects = false;
            return sdevice;
        }
        public void SetDevice(Microsoft.DirectX.Direct3D.Device Dev)
        {
            device = Dev;
            OFont = new System.Drawing.Font("Arial", 12);
            Font = new Microsoft.DirectX.Direct3D.Font(device, OFont);
            device.SamplerState[0].MagFilter = Filtering;
            device.SamplerState[0].MinFilter = Filtering;
            device.SamplerState[0].MipFilter = Filtering;
            device.SamplerState[0].MaxMipLevel = MipCount;
            for (int i = 0; i < CameraAt.Length; i++)
            {
                CameraAt[i] = new Vector3(0f, 0f, 0f);
                CameraLook[i] = new Vector3(0f, 0f, 0f);
                CameraUp[i] = new Vector3(0f, 0f, 1f);
            }
            device.RenderState.Ambient = AmbientColor;
            device.RenderState.Lighting = Lighting;
            device.RenderState.CullMode = Culling;
            device.RenderState.ZBufferEnable = true;
            /*device.SetRenderState(RenderStates.ZEnable, true);
            device.SetRenderState(RenderStates.ZBufferWriteEnable, true);
            device.SetRenderState(RenderStates.ZBufferFunction, true);
            device.RenderState.ZBufferEnable = true;
            device.RenderState.ZBufferWriteEnable = true;
            device.RenderState.ZBufferFunction = Compare.LessEqual;*/
            Material.Ambient = Color.White;
            Material.Diffuse = Color.White;
            device.RenderState.SpecularEnable = true;
        }
        public void SetInputDevice(Microsoft.DirectX.DirectInput.Device Dev)
        {
            kbdevice = Dev;
        }
        public void SetMouseDevice(Microsoft.DirectX.DirectInput.Device Dev)
        {
            msdevice = Dev;
        }
        public void SetSoundDevice(Microsoft.DirectX.DirectSound.Device Dev)
        {
            sdevice = Dev;
        }
        public float Framerate()
        {
            Frames++;
            if (Math.Abs(Environment.TickCount - LastTickCount) > 1000)
            {
                LastFrameRate = (float)Frames * 1000 / Math.Abs(Environment.TickCount - LastTickCount);
                LastTickCount = Environment.TickCount;
                Frames = 0;
            }
            return LastFrameRate;
        }
        public void Projection()
        {
            device.Transform.Projection = Matrix.PerspectiveFovLH((float)Math.PI / 4, (float)((float)Wnd.Size.Width / (float)Wnd.Size.Height), NearRender, FarRender);
            device.Transform.View = Matrix.LookAtLH(CameraAt[CameraOn], CameraLook[CameraOn], new Vector3(0f, 0f, 1f));
            CamAt = CameraAt[CameraOn];
            CamLook = CameraLook[CameraOn];
            //CamUp = CameraUp[CameraOn];
        }
        public bool KeyPress(Key KBKey)
        {
            bool chk = false;
            if (kbdevice != null)
            {
                if (kbState[KBKey]) { chk = true; }
            }
            return chk;
        }
        public bool KeyPressed(Key KBKey)
        {
            bool chk = false;
            if (kbdevice != null)
            {
                if (kbState[KBKey] && LastKBKey != KBKey)
                {
                    LastKBKey = KBKey;
                    chk = true;
                }
            }
            return chk;
        }
        public void SceneStart(Color BGColor)
        {
            if (kbdevice != null)
            {
                kbState = kbdevice.GetCurrentKeyboardState();
            }
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, BGColor, 1.0f, 0);
            device.SamplerState[0].MagFilter = Filtering;
            device.SamplerState[0].MinFilter = Filtering;
            device.SamplerState[0].MipFilter = Filtering;
            device.SamplerState[0].MaxMipLevel = MipCount;
            device.SetRenderState(RenderStates.Ambient, true);
            device.RenderState.ZBufferEnable = true;
            /*device.SetRenderState(RenderStates.ZEnable, true);
            device.SetRenderState(RenderStates.ZBufferWriteEnable, true);
            device.SetRenderState(RenderStates.ZBufferFunction, true);
            device.RenderState.ZBufferEnable = true;
            device.RenderState.ZBufferWriteEnable = true;
            device.RenderState.ZBufferFunction = Compare.LessEqual;*/
            device.RenderState.Ambient = AmbientColor;
            device.RenderState.Lighting = Lighting;
            device.RenderState.CullMode = Culling;
            /*device.RenderState.DestinationBlend = Blend.One;
            device.RenderState.AlphaBlendEnable = true;
            device.RenderState.SourceBlend = Blend.One;*/
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].Collide)
                {
                    Objects[i].CollideID = AddRealCollideID(Objects[i].Position - Objects[i].Pivot, Objects[i].Size);
                    CollideName[Objects[i].CollideID] = Objects[i].Name;
                    Collides[Objects[i].CollideID].Name = CollideName[Objects[i].CollideID];
                }
            }
            for (int i = 0; i < Timer.Count; i++)
            {
                Timer[i].Frame++;
            }
            SecondFrame++;
            device.BeginScene();
            for (int i = 0; i < Effect.Count; i++)
            {
                for (int ii = 0; ii < Effect[i].Instance.Count; ii++)
                {
                    if (CheckPosition(Effect[i].Instance[ii], CamAt - new Vector3(FarRender, FarRender, FarRender), CamAt + new Vector3(FarRender, FarRender, FarRender)))
                    {
                        Material Mat = new Material();
                        Mat.Ambient = Color.Blue;
                        Mat.Diffuse = Color.Blue;
                        Mat.Emissive = Color.Blue;
                        Mat.Specular = Color.Blue;
                        if (Effect[i].Type == Variable.EFFECT_RAIN)
                        {
                            CustomVertex.PositionColored[] vtx = new CustomVertex.PositionColored[5];
                            vtx[0].Position = new Vector3(Effect[i].Instance[ii].X, Effect[i].Instance[ii].Y, Effect[i].Instance[ii].Z);
                            vtx[0].Color = Color.Blue.ToArgb();
                            vtx[1].Position = new Vector3(Effect[i].Instance[ii].X + 1f, Effect[i].Instance[ii].Y, Effect[i].Instance[ii].Z);
                            vtx[1].Color = Color.Blue.ToArgb();
                            vtx[2].Position = new Vector3(Effect[i].Instance[ii].X + 1f, Effect[i].Instance[ii].Y, Effect[i].Instance[ii].Z - 2f);
                            vtx[2].Color = Color.Blue.ToArgb();
                            vtx[3].Position = new Vector3(Effect[i].Instance[ii].X, Effect[i].Instance[ii].Y, Effect[i].Instance[ii].Z - 2f);
                            vtx[3].Color = Color.Blue.ToArgb();
                            vtx[4].Position = new Vector3(Effect[i].Instance[ii].X, Effect[i].Instance[ii].Y, Effect[i].Instance[ii].Z);
                            vtx[4].Color = Color.Blue.ToArgb();
                            device.Material = Mat;
                            device.RenderState.CullMode = Cull.None;
                            device.DrawUserPrimitives(PrimitiveType.TriangleFan, vtx.Length - 1, vtx);
                            device.RenderState.CullMode = Culling;
                        }
                    }
                }
            }
            device.Material = Material;
            /*for (int i = 0; i < Light.Count; i++)
            {
                device.Lights[i].Type = Light[i].Type;
                device.Lights[i].Ambient = Light[i].Color;
                device.Lights[i].Diffuse = Light[i].Color;
                device.Lights[i].Specular = Color.White;
                device.Lights[i].Range = Light[i].Range;
                device.Lights[i].Position = Light[i].Position;
                device.Lights[i].Attenuation0 = 0.1f;
                device.Lights[i].Attenuation1 = Att1;
                device.Lights[i].Attenuation2 = Att2;
                device.Lights[i].Enabled = true;
            }*/
        }
        public void SceneEnd()
        {
            Triangles = 0;
            Points = 0;
            int Strength = 0;
            float PerStrength = 0, PerPoint = CollidePerPoint;
            Vector2 Ranges;
            Vector3 RandPos;
            for (int i = 0; i < Effect.Count; i++)
            {
                if (Effect[i].Speed < 1f) { CollidePerPoint = Effect[i].Speed; } { CollidePerPoint = 1f; }
                Strength = Effect[i].Strength;
                Ranges = new Vector2(Effect[i].To.X - Effect[i].From.X, Effect[i].To.Y - Effect[i].From.Y);
                List<float> MMake = new List<float>();
                PerStrength = 60f / Strength;
                for (int ii = 0; ii < Strength; ii++) { MMake.Add(PerStrength * (ii + 1)); }
                for (int ii = 0; ii < MMake.Count; ii++)
                {
                    RandPos = new Vector3(Effect[i].From.X + (float)Random.Next(0, (int)Ranges.X), Effect[i].From.Y + (float)Random.Next(0, (int)Ranges.Y), Effect[i].From.Z);
                    if (SecondFrame == (int)Math.Round(MMake[ii]) && CheckPosition(RandPos, CamAt - new Vector3(FarRender, FarRender, FarRender), CamAt + new Vector3(FarRender, FarRender, FarRender)))
                    {
                        Effect[i].Instance.Add(RandPos);
                    }
                }
                for (int ii = 0; ii < Effect[i].Instance.Count; ii++)
                {
                    if (CheckCollide(new Vector3(Effect[i].Instance[ii].X, Effect[i].Instance[ii].Y, Effect[i].Instance[ii].Z - Effect[i].Speed), new Vector3(1f, 1f, Effect[i].Speed))!=-1 && CheckPosition(new Vector3(Effect[i].Instance[ii].X, Effect[i].Instance[ii].Y, Effect[i].Instance[ii].Z - Effect[i].Speed), new Vector3(Effect[i].From.X, Effect[i].From.Y, Effect[i].To.Z), new Vector3(Effect[i].To.X, Effect[i].To.Y, Effect[i].From.Z)))
                    {
                        Effect[i].Instance[ii] = new Vector3(Effect[i].Instance[ii].X, Effect[i].Instance[ii].Y, Effect[i].Instance[ii].Z - Effect[i].Speed);
                    }
                    else { Effect[i].Instance.RemoveAt(ii); }
                }
            }
            CollidePerPoint = PerPoint;
            if (SecondFrame == 60) { SecondFrame = 0; }
            if (CameraTasks.Count > 0 && CameraTasks.Count > CameraTasksAt)
            {
                CameraTsk tsk = CameraTasks[CameraTasksAt];
                if (tsk.Frames != tsk.MaxFrames)
                {
                    CameraAt[CameraOn].X += tsk.Speed1.X;
                    CameraAt[CameraOn].Y += tsk.Speed1.Y;
                    CameraAt[CameraOn].Z += tsk.Speed1.Z;
                    CameraLook[CameraOn].X += tsk.Speed2.X;
                    CameraLook[CameraOn].Y += tsk.Speed2.Y;
                    CameraLook[CameraOn].Z += tsk.Speed2.Z;
                    tsk.Frames++;
                    CamTask = true;
                }
                else { CameraTasksAt++; }
            }
            else if (CameraTasks.Count == CameraTasksAt) { CameraTasks.Clear(); CameraTasksAt = 0; CamTask = false; }
            device.EndScene();
            device.Present();
            for (int i = 0; i < Lights; i++)
            {
                device.Lights[i].Enabled = false;
            }
            Lights = 0;
            for (int i = 0; i < FakeCollides.Count; i++)
            {
                Collides.RemoveAt(FakeCollides[i]);
            }
            FakeCollides.Clear();
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].Physics)
                {
                    if (CheckCollideBoth(Objects[i].Position-Objects[i].Pivot + Objects[i].PositionSpeed, Objects[i].Size, new int[] { Objects[i].CollideID }, Objects[i].IgnoreNames.ToArray())==-1)
                    {
                        Objects[i].Position += Objects[i].PositionSpeed;
                        Objects[i].Rotation += Objects[i].RotationSpeed;
                        Objects[i].PositionSpeed.Z -= Objects[i].Gravity;
                        Objects[i].Status = Variable.STATUS_FLYING;
                    }
                    else
                    {
                        if (Objects[i].PositionSpeed != new Vector3(0f, 0f, 0f)) { Objects[i].PositionSpeed = new Vector3(0f, 0f, 0f); }
                    }
                    /*if (!CheckCollideZ(new Vector3(Objects[i].Position.X, Objects[i].Position.Y, Objects[i].Position.Z + Objects[i].PositionSpeed.Z), Objects[i].Size))
                    {
                        Objects[i].PositionSpeed.Z = 0f;
                    }*/
                    if (Objects[i].LastPosition.Z == Objects[i].Position.Z)
                    {
                        Objects[i].Status = Variable.STATUS_STANDING;
                    }
                }
                Objects[i].LastPosition = Objects[i].Position;
                Objects[i].LastPositionSpeed = Objects[i].LastPositionSpeed;
                Objects[i].LastRotation = Objects[i].Rotation;
                Objects[i].LastRotationSpeed = Objects[i].RotationSpeed;
            }
            Collides.Clear();
            Collide = true;
            MouseOld = new Vector2(MouseGet(Variable.MOUSE_X), MouseGet(Variable.MOUSE_Y));
            if (kbdevice != null)
            {
                if (LastKBKey != Key.Yen)
                {
                    if (!kbState[LastKBKey]) { LastKBKey = Key.Yen; }
                }
            }
            for (int i = 0; i < Timer.Count; i++)
            {
                if (Timer[i].Frame == Timer[i].Interval) { Timer[i].Frame = 0; }
            }
            Wnd.Invalidate();
        }
        public void PrimitiveBegin()
        {
            PrimitiveTexture = null;
        }
        public void PrimitiveBeginTexture(Texture Tex)
        {
            PrimitiveTexture = Tex;
        }
        public void Vertex(Vector3 Position, float Tu = 0, float Tv = 0)
        {
            PrimitiveVertex.Add(new Vector3(Position.X, Position.Y, Position.Z));
            PrimitiveVertexColor.Add(Color.White);
            PrimitiveTexs.Add(new Vector2(Tu, Tv));
        }
        public void VertexColor(Vector3 Position, Color Clr)
        {
            PrimitiveVertex.Add(new Vector3(Position.X, Position.Y, Position.Z));
            PrimitiveVertexColor.Add(Clr);
        }
        public void PrimitiveEnd()
        {
            if (PrimitiveVertex.Count != 0)
            {
                if (Camera)
                {
                    if (PrimitiveTexture != null)
                    {
                        if (Lighting)
                        {
                            CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[PrimitiveVertex.Count];
                            device.Material = Material;
                            device.SetTexture(0, PrimitiveTexture);
                            for (int i = 0; i < PrimitiveVertex.Count; i++)
                            {
                                vtx[i].Position = PrimitiveVertex[i];
                                vtx[i].Tu = PrimitiveTexs[i].X;
                                vtx[i].Tv = PrimitiveTexs[i].Y;
                                vtx[i].Normal = new Vector3(0f, 0f, -1f);
                            }
                            device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                            try
                            {
                                device.DrawUserPrimitives(PrimitiveType.TriangleStrip, PrimitiveVertex.Count, vtx);
                            }
                            catch { }
                        }
                        else
                        {
                            CustomVertex.PositionTextured[] vtx = new CustomVertex.PositionTextured[PrimitiveVertex.Count];
                            device.Material = Material;
                            device.SetTexture(0, PrimitiveTexture);
                            for (int i = 0; i < PrimitiveVertex.Count; i++)
                            {
                                vtx[i].Position = PrimitiveVertex[i];
                                vtx[i].Tu = PrimitiveTexs[i].X;
                                vtx[i].Tv = PrimitiveTexs[i].Y;
                            }
                            device.VertexFormat = CustomVertex.PositionTextured.Format;
                            try
                            {
                                device.DrawUserPrimitives(PrimitiveType.TriangleStrip, PrimitiveVertex.Count, vtx);
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        if (Lighting)
                        {
                            CustomVertex.PositionNormalColored[] vtx = new CustomVertex.PositionNormalColored[PrimitiveVertex.Count];
                            for (int i = 0; i < PrimitiveVertex.Count; i++)
                            {
                                vtx[i].Position = PrimitiveVertex[i];
                                vtx[i].Color = PrimitiveVertexColor[i].ToArgb();
                                vtx[i].Normal = new Vector3(0f, 0f, -1f);
                            }
                            device.VertexFormat = CustomVertex.PositionNormalColored.Format;
                            try
                            {
                                device.DrawUserPrimitives(PrimitiveType.TriangleStrip, PrimitiveVertex.Count, vtx);
                            }
                            catch { }
                        }
                        else
                        {
                            CustomVertex.PositionColored[] vtx = new CustomVertex.PositionColored[PrimitiveVertex.Count];
                            for (int i = 0; i < PrimitiveVertex.Count; i++)
                            {
                                vtx[i].Position = PrimitiveVertex[i];
                                vtx[i].Color = PrimitiveVertexColor[i].ToArgb();
                            }
                            device.VertexFormat = CustomVertex.PositionColored.Format;
                            try
                            {
                                device.DrawUserPrimitives(PrimitiveType.TriangleStrip, PrimitiveVertex.Count, vtx);
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    CustomVertex.TransformedColored[] vtx = new CustomVertex.TransformedColored[PrimitiveVertex.Count];
                    for (int i = 0; i < PrimitiveVertex.Count; i++)
                    {
                        vtx[i].Position = new Vector4(PrimitiveVertex[i].X, PrimitiveVertex[i].Y, PrimitiveVertex[i].Z, 0);
                        vtx[i].Color = PrimitiveVertexColor[i].ToArgb();
                    }
                    device.SetTexture(0, PrimitiveTexture);
                    device.VertexFormat = CustomVertex.TransformedColored.Format;
                }
            }
        }
        public void CreateCamera()
        {
            Camera = true;
            device.RenderState.Lighting = false;
            device.RenderState.CullMode = Cull.None;
        }
        public void CameraPosition(Vector3 Position)
        {
            CameraAt[CameraOn] = new Vector3(Position.X, Position.Y, Position.Z);
            CamAt = CameraAt[CameraOn];
            CamLook = CameraLook[CameraOn];
            CamUp = CameraUp[CameraOn];
        }
        public void CameraLookAt(Vector3 LookAt)
        {
            CameraLook[CameraOn] = new Vector3(LookAt.X, LookAt.Y, LookAt.Z);
            CamAt = CameraAt[CameraOn];
            CamLook = CameraLook[CameraOn];
            CamUp = CameraUp[CameraOn];
        }
        public void CameraLookUp(Vector3 Up)
        {
            CameraUp[CameraOn] = Up;
            CamAt = CameraAt[CameraOn];
            CamLook = CameraLook[CameraOn];
            CamUp = CameraUp[CameraOn];
        }
        public void CameraID(int ID)
        {
            if (ID >= 0 && ID < 100) { CameraOn = ID; }
        }
        public void World(Matrix Mtx)
        {
            device.Transform.World = Mtx;
        }
        public Matrix WorldMatrix(int Rotation, float Angle, Vector3 Axis = default(Vector3))
        {
            if (Rotation == Variable.ROTATION_X)
            {
                WorldRotation = new Vector3(Angle, WorldRotation.Y, WorldRotation.Z);
                return Matrix.RotationX(Angle);
            }
            else if (Rotation == Variable.ROTATION_Y)
            {
                WorldRotation = new Vector3(WorldRotation.X, Angle, WorldRotation.Z);
                return Matrix.RotationY(Angle);
            }
            else if (Rotation == Variable.ROTATION_Z)
            {
                WorldRotation = new Vector3(WorldRotation.X, WorldRotation.Y, Angle);
                return Matrix.RotationZ(Angle);
            }
            else if (Rotation == Variable.ROTATION_AXIS)
            {
                WorldAxis = new Vector4(Axis.X, Axis.Y, Axis.Z, Angle);
                return Matrix.RotationAxis(Axis, Angle);
            }
            else if (Rotation == Variable.TRANSLATION)
            {
                WorldTranslation = new Vector3(Axis.X, Axis.Y, Axis.Z);
                return Matrix.Translation(Axis);
            }
            return Matrix.Zero;
        }
        public void WorldPhysics(bool Value)
        {
            Physics = Value;
        }
        public void WorldGravity(float Value)
        {
            Gravity = Value;
        }
        public Vector3 CameraGetPosition()
        {
            return CameraAt[CameraOn];
        }
        public Vector3 CameraGetLookAt()
        {
            return CameraLook[CameraOn];
        }
        public Vector3 CameraGetLookUp()
        {
            return CameraUp[CameraOn];
        }
        public void CameraRender(int Var, float Value)
        {
            if (Var == Variable.RENDER_NEAR)
            {
                NearRender = Value;
            }
            else if (Var == Variable.RENDER_FAR)
            {
                FarRender = Value;
            }
        }
        public float WorldGetMatrix(int Var)
        {
            if (Var == Variable.ROTATION_X)
            {
                return WorldRotation.X;
            }
            else if (Var == Variable.ROTATION_Y)
            {
                return WorldRotation.Y;
            }
            else if (Var == Variable.ROTATION_Z)
            {
                return WorldRotation.Z;
            }
            else if (Var == Variable.ROTATION_AXIS_W)
            {
                return WorldAxis.W;
            }
            return 0f;
        }
        public Vector3 WorldGetVector(int Var)
        {
            if (Var == Variable.ROTATION_AXIS)
            {
                return new Vector3(WorldAxis.X, WorldAxis.Y, WorldAxis.Z);
            }
            else if (Var == Variable.TRANSLATION)
            {
                return WorldTranslation;
            }
            return new Vector3(0, 0, 0);
        }
        public void WorldMaterial(Material Mat)
        {
            Material = Mat;
        }
        public void WorldMaterialDefault()
        {
            Material.Ambient = Color.White;
            Material.Diffuse = Color.White;
            Material.Emissive = Color.Black;
            Material.Specular = Color.Black;
        }
        public Material WorldMaterialGet()
        {
            return Material;
        }
        public TerrainObj TerrainBegin(Vector3 Position, Vector2 Size, float Space = 1f, Texture Tex = null, float Tu = 1, float Tv = 1)
        {
            TerrainObj Obj = new TerrainObj(Position, Size);
            Obj.Texture = Tex;
            Obj.Space = Space;
            Terrains.Add(Obj);
            return Terrains[Terrains.Count - 1];
        }
        /*public void Terrain(Vector2 Pos, float Z)
        {
            Terrains[Terrains.Count - 1].Set(Pos, Z);
        }*/
        public void TerrainEnd()
        {
            float Space = Terrains[Terrains.Count - 1].Space;
            float X = Terrains[Terrains.Count - 1].Position.X, Y = Terrains[Terrains.Count - 1].Position.Y, Z = Terrains[Terrains.Count - 1].Position.Z, X2 = Terrains[Terrains.Count - 1].Position.X + Terrains[Terrains.Count - 1].Size.X, Y2 = Terrains[Terrains.Count - 1].Position.Y + Terrains[Terrains.Count - 1].Size.Y;
            float Tu = Terrains[Terrains.Count - 1].Tu, Tv = Terrains[Terrains.Count - 1].Tv;
            CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[(int)(Math.Abs(Math.Round((X2 - X) / Space))) * (int)(Math.Abs(Math.Round((Y2 - Y) / Space))) * 6];
            int at = 0;
            float[,] ZZ = new float[(int)(Math.Abs(Math.Round((X2 - X) / Space))) + 2, (int)(Math.Abs(Math.Round((Y2 - Y) / Space))) + 2];
            for (int i = 0; i < (int)(Math.Abs(Math.Round((X2 - X) / Space)))+2; i += 1)
            {
                for (int ii = 0; ii < (int)(Math.Abs(Math.Round((Y2 - Y) / Space)))+2; ii += 1)
                {
                    ZZ[i, ii] = Z;
                }
            }
            for (int i = 0; i < Terrains[Terrains.Count - 1].HeightData.Count; i++)
            {
                ZZ[(int)Terrains[Terrains.Count - 1].HeightData[i].X, (int)Terrains[Terrains.Count - 1].HeightData[i].Y] = Z + Terrains[Terrains.Count - 1].HeightData[i].Z;
            }
            //Draw
            bool can = true;
            float o, oo;
            for (float i = X; i < X2; i += Space)
            {
                o = Math.Abs(i-X);
                if (i >= X2) { can=false; }
                if (can)
                {
                    for (float ii = Y; ii < Y2; ii += Space)
                    {
                        oo = Math.Abs(ii-Y);
                        if (ii >= Y2) { can = false; }
                        if (can)
                        {
                            vtx[at].Position = new Vector3(i, ii, ZZ[(int)(o / Space), (int)(oo / Space)]);
                            vtx[at].Tu = 0;
                            vtx[at].Tv = 0;
                            vtx[at + 1].Position = new Vector3(i + Space, ii, ZZ[(int)(o / Space + 1f), (int)(oo / Space)]);
                            vtx[at + 1].Tu = Tu;
                            vtx[at + 1].Tv = 0;
                            vtx[at + 2].Position = new Vector3(i, ii + Space, ZZ[(int)(o / Space), (int)(oo / Space + 1f)]);
                            vtx[at + 2].Tu = 0;
                            vtx[at + 2].Tv = Tv;
                            vtx[at + 3].Position = new Vector3(i + Space, ii + Space, ZZ[(int)(o / Space + 1f), (int)(oo / Space + 1f)]);
                            vtx[at + 3].Tu = Tu;
                            vtx[at + 3].Tv = Tv;
                            vtx[at + 4].Position = new Vector3(i, ii + Space, ZZ[(int)(o / Space), (int)(oo / Space + 1f)]);
                            vtx[at + 4].Tu = 0;
                            vtx[at + 4].Tv = Tv;
                            vtx[at + 5].Position = new Vector3(i + Space, ii, ZZ[(int)(o / Space + 1f), (int)(oo / Space)]);
                            vtx[at + 5].Tu = Tu;
                            vtx[at + 5].Tv = 0;

                            vtx[at].Normal = new Vector3(0f, 0f, 1f);
                            vtx[at + 1].Normal = new Vector3(1f, 0f, 1f);
                            vtx[at + 2].Normal = new Vector3(0f, 1f, 1f);
                            vtx[at + 3].Normal = new Vector3(1f, 1f, 1f);
                            vtx[at + 4].Normal = new Vector3(0f, 1f, 1f);
                            vtx[at + 5].Normal = new Vector3(1f, 0f, 1f);
                            at += 6;
                        }
                    }
                }
                can = true;
            }
            //----
            device.SetTexture(0, Terrains[Terrains.Count - 1].Texture);
            device.Material = Material;
            device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
            device.DrawUserPrimitives(PrimitiveType.TriangleList, vtx.Length / 3, vtx);
            Points += vtx.Length;
            Triangles += vtx.Length / 3;
        }
        public void TerrainFile(string Filename)
        {
            //Not Completed
        }
        public void WorldLighting(bool Value)
        {
            device.RenderState.Lighting = Value;
            Lighting = Value;
        }
        public void WorldCulling(Cull Value)
        {
            device.RenderState.CullMode = Value;
            Culling = Value;
        }
        public void Floor(Vector3 Position, Vector3 To, Texture Tex)
        {
            device.RenderState.CullMode = Cull.None;
            if (Camera)
            {
                float X = Position.X, Y = Position.Y, Z = Position.Z, X2 = To.X, Y2 = To.Y, Z2 = To.Z;
                CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[5];
                device.Material = Material;
                device.SetTexture(0, Tex);
                //Draw
                vtx[0].Position = new Vector3(X, Y, Z);
                vtx[0].Tu = 0;
                vtx[0].Tv = 0;
                vtx[1].Position = new Vector3(X2, Y, Z);
                vtx[1].Tu = 1;
                vtx[1].Tv = 0;
                vtx[2].Position = new Vector3(X2, Y2, Z2);
                vtx[2].Tu = 1;
                vtx[2].Tv = 1;
                vtx[3].Position = new Vector3(X, Y2, Z2);
                vtx[3].Tu = 0;
                vtx[3].Tv = 1;
                vtx[4].Position = new Vector3(X, Y, Z);
                vtx[4].Tu = 0;
                vtx[4].Tv = 0;

                //Normal
                vtx[0].Normal = new Vector3(0f, 0f, 0f);
                vtx[1].Normal = new Vector3(1f, 0f, 0f);
                vtx[2].Normal = new Vector3(1f, 1f, 1f);
                vtx[3].Normal = new Vector3(0f, 1f, 1f);
                vtx[4].Normal = new Vector3(0f, 0f, 0f);
                //----
                //----

                device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                try
                {
                    device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleFan, 4, vtx);
                }
                catch { }

                if (Collide)
                {
                    Collides.Add(new CollideObj(new Vector3(TransformPos.X + X, TransformPos.Y + Y, TransformPos.Z + Z - 0.1f), new Vector3(TransformPos.X + X2 - X, TransformPos.Y + Y2 - Y, TransformPos.Z + Z2 - Z + 0.1f)));
                    CollideName.Add("");
                }
                Triangles += 2;
                Points += 6;
            }
            device.RenderState.CullMode = Culling;
        }
        public void FloorUV(Vector3 Position, Vector3 To, Texture Tex, float U, float V)
        {
            device.RenderState.CullMode = Cull.None;
            if (Camera)
            {
                float X = Position.X, Y = Position.Y, Z = Position.Z, X2 = To.X, Y2 = To.Y, Z2 = To.Z;
                CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[6];
                device.Material = Material;
                device.SetTexture(0, Tex);
                //Draw
                vtx[0].Position = new Vector3(X, Y, Z);
                vtx[0].Tu = 0;
                vtx[0].Tv = 0;
                vtx[1].Position = new Vector3(X2, Y, Z);
                vtx[1].Tu = U;
                vtx[1].Tv = 0;
                vtx[2].Position = new Vector3(X, Y2, Z2);
                vtx[2].Tu = 0;
                vtx[2].Tv = V;
                vtx[3].Position = new Vector3(X2, Y2, Z2);
                vtx[3].Tu = U;
                vtx[3].Tv = V;
                vtx[4].Position = new Vector3(X, Y2, Z2);
                vtx[4].Tu = 0;
                vtx[4].Tv = V;
                vtx[5].Position = new Vector3(X2, Y, Z);
                vtx[5].Tu = U;
                vtx[5].Tv = 0;

                //Normal
                vtx[0].Normal = new Vector3(0f, 0f, 0f);
                vtx[1].Normal = new Vector3(1f, 0f, 0f);
                vtx[2].Normal = new Vector3(0f, 1f, 1f);
                vtx[3].Normal = new Vector3(1f, 1f, 1f);
                vtx[4].Normal = new Vector3(0f, 1f, 1f);
                vtx[5].Normal = new Vector3(1f, 0f, 0f);
                //----
                //----

                device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                try
                {
                    device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, 2, vtx);
                }
                catch { }

                if (Collide)
                {
                    Collides.Add(new CollideObj(new Vector3(TransformPos.X + X, TransformPos.Y + Y, TransformPos.Z + Z - 0.1f), new Vector3(TransformPos.X + X2 - X, TransformPos.Y + Y2 - Y, TransformPos.Z + Z2 - Z + 0.1f)));
                    CollideName.Add("");
                }
                Triangles += 2;
                Points += 6;
            }
            device.RenderState.CullMode = Culling;
        }
        public void FloorParted(Vector3 Position, Vector3 To, Texture Tex, float U, float V)
        {
            device.RenderState.CullMode = Cull.None;
            if (Camera)
            {
                float X = Position.X, Y = Position.Y, Z = Position.Z, X2 = To.X, Y2 = To.Y, Z2 = To.Z, W = (float)Math.Round((To.X - Position.X) / U), H = (float)Math.Round((To.Y - Position.Y) / V);
                int iid = 0;
                CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[(int)((U+1) * (V+1)) * 6];
                device.Material = Material;
                device.SetTexture(0, Tex);
                //Draw
                for (float i = X; i < X2; i += W)
                {
                    for (float ii = Y; ii < Y2; ii += H)
                    {
                            vtx[iid].Position = new Vector3(i, ii, Z);
                            vtx[iid].Tu = 0;
                            vtx[iid].Tv = 0;
                            vtx[iid + 1].Position = new Vector3(i + W, ii, Z);
                            vtx[iid + 1].Tu = 1;
                            vtx[iid + 1].Tv = 0;
                            vtx[iid + 2].Position = new Vector3(i, ii + H, Z2);
                            vtx[iid + 2].Tu = 0;
                            vtx[iid + 2].Tv = 1;
                            vtx[iid + 3].Position = new Vector3(i + W, ii + H, Z2);
                            vtx[iid + 3].Tu = 1;
                            vtx[iid + 3].Tv = 1;
                            vtx[iid + 4].Position = new Vector3(i, ii + H, Z2);
                            vtx[iid + 4].Tu = 0;
                            vtx[iid + 4].Tv = 1;
                            vtx[iid + 5].Position = new Vector3(i + W, ii, Z);
                            vtx[iid + 5].Tu = 1;
                            vtx[iid + 5].Tv = 0;
                            vtx[iid].Normal = new Vector3(0f, 0f, 0f);
                            vtx[iid + 1].Normal = new Vector3(1f, 0f, 0f);
                            vtx[iid + 2].Normal = new Vector3(0f, 1f, 1f);
                            vtx[iid + 3].Normal = new Vector3(1f, 1f, 1f);
                            vtx[iid + 4].Normal = new Vector3(0f, 1f, 1f);
                            vtx[iid + 5].Normal = new Vector3(1f, 0f, 0f);
                            iid += 6;
                            Triangles += 2;
                            Points += 6;
                    }
                }
                //----

                device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                try
                {
                    device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, vtx.Length / 3, vtx);
                }
                catch { }

                if (Collide)
                {
                    Collides.Add(new CollideObj(new Vector3(TransformPos.X + X, TransformPos.Y + Y, TransformPos.Z + Z - 0.1f), new Vector3(TransformPos.X + X2 - X, TransformPos.Y + Y2 - Y, TransformPos.Z + Z2 - Z + 0.1f)));
                    CollideName.Add("");
                }
            }
            device.RenderState.CullMode = Culling;
        }
        public void FloorUVW(Vector3 Position, Vector3 To, Texture Tex, float SU, float SV, float EU, float EV)
        {
            device.RenderState.CullMode = Cull.None;
            if (Camera)
            {
                float X = Position.X, Y = Position.Y, Z = Position.Z, X2 = To.X, Y2 = To.Y, Z2 = To.Z;
                CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[5];
                device.Material = Material;
                device.SetTexture(0, Tex);
                //Draw
                vtx[0].Position = new Vector3(X, Y, Z);
                vtx[0].Tu = SU;
                vtx[0].Tv = SV;
                vtx[1].Position = new Vector3(X2, Y, Z);
                vtx[1].Tu = EU;
                vtx[1].Tv = SV;
                vtx[2].Position = new Vector3(X2, Y2, Z2);
                vtx[2].Tu = EU;
                vtx[2].Tv = EV;
                vtx[3].Position = new Vector3(X, Y2, Z2);
                vtx[3].Tu = SU;
                vtx[3].Tv = EV;
                vtx[4].Position = new Vector3(X, Y, Z);
                vtx[4].Tu = SU;
                vtx[4].Tv = SV;

                //Normal
                vtx[0].Normal = new Vector3(0f, 0f, 0f);
                vtx[1].Normal = new Vector3(1f, 0f, 0f);
                vtx[2].Normal = new Vector3(1f, 1f, 1f);
                vtx[3].Normal = new Vector3(0f, 1f, 1f);
                vtx[4].Normal = new Vector3(0f, 0f, 0f);
                //----
                //----

                device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                try
                {
                    device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleFan, 4, vtx);
                }
                catch { }

                if (Collide)
                {
                    Collides.Add(new CollideObj(new Vector3(TransformPos.X + X, TransformPos.Y + Y, TransformPos.Z + Z - 0.1f), new Vector3(TransformPos.X + X2 - X, TransformPos.Y + Y2 - Y, TransformPos.Z + Z2 - Z + 0.1f)));
                    CollideName.Add("");
                }
                Triangles += 2;
                Points += 6;
            }
            device.RenderState.CullMode = Culling;
        }
        public void Wall(Vector3 Position, Vector3 To, Texture Tex)
        {
            device.RenderState.CullMode = Cull.None;
            if (Camera)
            {
                float X = Position.X, Y = Position.Y, Z = Position.Z, X2 = To.X, Y2 = To.Y, Z2 = To.Z;
                CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[5];
                device.Material = Material;
                device.SetTexture(0, Tex);
                //Draw
                vtx[0].Position = new Vector3(X, Y, Z);
                vtx[0].Tu = 0;
                vtx[0].Tv = 0;
                vtx[1].Position = new Vector3(X2, Y2, Z);
                vtx[1].Tu = 1;
                vtx[1].Tv = 0;
                vtx[2].Position = new Vector3(X2, Y2, Z2);
                vtx[2].Tu = 1;
                vtx[2].Tv = 1;
                vtx[3].Position = new Vector3(X, Y, Z2);
                vtx[3].Tu = 0;
                vtx[3].Tv = 1;
                vtx[4].Position = new Vector3(X, Y, Z);
                vtx[4].Tu = 0;
                vtx[4].Tv = 0;

                //Normals
                vtx[0].Normal = new Vector3(0f, 0f, 0f);
                vtx[1].Normal = new Vector3(1f, 1f, 0f);
                vtx[2].Normal = new Vector3(1f, 1f, 1f);
                vtx[3].Normal = new Vector3(0f, 0f, 1f);
                vtx[4].Normal = new Vector3(0f, 0f, 0f);
                //----
                //----

                device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                try
                {
                    device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleFan, 4, vtx);
                }
                catch { }

                if (Collide)
                {
                    Collides.Add(new CollideObj(new Vector3(TransformPos.X + X - 0.1f, TransformPos.Y + Y - 0.1f, TransformPos.Z + Z), new Vector3(TransformPos.X + X2 - X + 0.1f, TransformPos.Y + Y2 - Y + 0.1f, TransformPos.Z + Z2 - Z)));
                    CollideName.Add("");
                }
                Triangles += 2;
                Points += 6;
            }
            device.RenderState.CullMode = Culling;
        }
        public void WallUV(Vector3 Position, Vector3 To, Texture Tex, float U, float V)
        {
            device.RenderState.CullMode = Cull.None;
            if (Camera)
            {
                float X = Position.X, Y = Position.Y, Z = Position.Z, X2 = To.X, Y2 = To.Y, Z2 = To.Z;
                CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[5];
                device.Material = Material;
                device.SetTexture(0, Tex);
                //Draw
                vtx[0].Position = new Vector3(X, Y, Z);
                vtx[0].Tu = 0;
                vtx[0].Tv = 0;
                vtx[1].Position = new Vector3(X2, Y2, Z);
                vtx[1].Tu = U;
                vtx[1].Tv = 0;
                vtx[2].Position = new Vector3(X2, Y2, Z2);
                vtx[2].Tu = U;
                vtx[2].Tv = V;
                vtx[3].Position = new Vector3(X, Y, Z2);
                vtx[3].Tu = 0;
                vtx[3].Tv = V;
                vtx[4].Position = new Vector3(X, Y, Z);
                vtx[4].Tu = 0;
                vtx[4].Tv = 0;

                //Normals
                vtx[0].Normal = new Vector3(0f, 0f, 0f);
                vtx[1].Normal = new Vector3(1f, 1f, 0f);
                vtx[2].Normal = new Vector3(1f, 1f, 1f);
                vtx[3].Normal = new Vector3(0f, 0f, 1f);
                vtx[4].Normal = new Vector3(0f, 0f, 0f);
                //----
                //----

                device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                try
                {
                    device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleFan, 4, vtx);
                }
                catch { }

                if (Collide)
                {
                    Collides.Add(new CollideObj(new Vector3(TransformPos.X + X - 0.1f, TransformPos.Y + Y - 0.1f, TransformPos.Z + Z), new Vector3(TransformPos.X + X2 - X + 0.1f, TransformPos.Y + Y2 - Y + 0.1f, TransformPos.Z + Z2 - Z)));
                    CollideName.Add("");
                }
                Triangles += 2;
                Points += 6;
            }
            device.RenderState.CullMode = Culling;
        }
        public void WallUVW(Vector3 Position, Vector3 To, Texture Tex, float SU, float SV, float EU, float EV)
        {
            device.RenderState.CullMode = Cull.None;
            if (Camera)
            {
                float X = Position.X, Y = Position.Y, Z = Position.Z, X2 = To.X, Y2 = To.Y, Z2 = To.Z;
                CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[5];
                device.Material = Material;
                device.SetTexture(0, Tex);
                //Draw
                vtx[0].Position = new Vector3(X, Y, Z);
                vtx[0].Tu = SU;
                vtx[0].Tv = SV;
                vtx[1].Position = new Vector3(X2, Y2, Z);
                vtx[1].Tu = EU;
                vtx[1].Tv = SV;
                vtx[2].Position = new Vector3(X2, Y2, Z2);
                vtx[2].Tu = EU;
                vtx[2].Tv = EV;
                vtx[3].Position = new Vector3(X, Y, Z2);
                vtx[3].Tu = SU;
                vtx[3].Tv = EV;
                vtx[4].Position = new Vector3(X, Y, Z);
                vtx[4].Tu = SU;
                vtx[4].Tv = SV;

                //Normals
                vtx[0].Normal = new Vector3(0f, 0f, 0f);
                vtx[1].Normal = new Vector3(1f, 1f, 0f);
                vtx[2].Normal = new Vector3(1f, 1f, 1f);
                vtx[3].Normal = new Vector3(0f, 0f, 1f);
                vtx[4].Normal = new Vector3(0f, 0f, 0f);
                //----
                //----

                device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                try
                {
                    device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleFan, 4, vtx);
                }
                catch { }

                if (Collide)
                {
                    Collides.Add(new CollideObj(new Vector3(TransformPos.X + X - 0.1f, TransformPos.Y + Y - 0.1f, TransformPos.Z + Z), new Vector3(TransformPos.X + X2 - X + 0.1f, TransformPos.Y + Y2 - Y + 0.1f, TransformPos.Z + Z2 - Z)));
                    CollideName.Add("");
                }
                Triangles += 2;
                Points += 6;
            }
            device.RenderState.CullMode = Culling;
        }
        public void WallParted(Vector3 Position, Vector3 To, Texture Tex, float U, float V)
        {
            device.RenderState.CullMode = Cull.None;
            float X = Position.X, Y = Position.Y, Z = Position.Z, X2 = To.X, Y2 = To.Y, Z2 = To.Z, W = (float)Math.Round((To.X - Position.X) / U), H = (float)Math.Round((To.Y - Position.Y) / U), D = (float)Math.Round((To.Z - Position.Z) / V);
            int iid = 0;
            CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[(int)((U + 1) * (V + 1)) * 6];
            device.Material = Material;
            device.SetTexture(0, Tex);
            //Draw
            for (float i = 0; i < U; i += 1f)
            {
                for (float ii = Z; ii < Z2; ii += D)
                {
                    vtx[iid].Position = new Vector3(X + (i * W), Y + (i * H), ii);
                    vtx[iid].Tu = 0;
                    vtx[iid].Tv = 0;
                    vtx[iid + 1].Position = new Vector3(X + (i * W) + W, Y + (i * H) + H, ii);
                    vtx[iid + 1].Tu = 1;
                    vtx[iid + 1].Tv = 0;
                    vtx[iid + 2].Position = new Vector3(X + (i * W) + W, Y + (i * H) + H, ii + D);
                    vtx[iid + 2].Tu = 1;
                    vtx[iid + 2].Tv = 1;
                    vtx[iid + 3].Position = new Vector3(X + (i * W), Y + (i * H), ii + D);
                    vtx[iid + 3].Tu = 0;
                    vtx[iid + 3].Tv = 1;
                    vtx[iid + 4].Position = new Vector3(X + (i * W) + W, Y + (i * H) + H, ii + D);
                    vtx[iid + 4].Tu = 1;
                    vtx[iid + 4].Tv = 1;
                    vtx[iid + 5].Position = new Vector3(X + (i * W), Y + (i * H), ii);
                    vtx[iid + 5].Tu = 0;
                    vtx[iid + 5].Tv = 0;
                    vtx[iid].Normal = new Vector3(0f, 0f, 0f);
                    vtx[iid + 1].Normal = new Vector3(1f, 1f, 0f);
                    vtx[iid + 2].Normal = new Vector3(1f, 1f, 1f);
                    vtx[iid + 3].Normal = new Vector3(0f, 0f, 1f);
                    vtx[iid + 4].Normal = new Vector3(1f, 1f, 1f);
                    vtx[iid + 5].Normal = new Vector3(0f, 0f, 0f);
                    iid += 6;
                }
            }
            //----

            device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
            try
            {
                device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, vtx.Length / 3, vtx);
            }
            catch { }
            if (Collide)
            {
                Collides.Add(new CollideObj(new Vector3(TransformPos.X + X - 0.1f, TransformPos.Y + Y - 0.1f, TransformPos.Z + Z), new Vector3(TransformPos.X + X2 - X + 0.1f, TransformPos.Y + Y2 - Y + 0.1f, TransformPos.Z + Z2 - Z)));
                CollideName.Add("");
            }
            Triangles += 2;
            Points += 6;
            device.RenderState.CullMode = Culling;
        }
        public void Cube(Vector3 Position, Vector3 To, Texture Tex)
        {
            if (Camera)
            {
                float X = Position.X, Y = Position.Y, Z = Position.Z, X2 = To.X, Y2 = To.Y, Z2 = To.Z;
                CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[36];
                device.Material = Material;
                device.SetTexture(0, Tex);
                //Draw
                vtx[0].Position = new Vector3(X, Y2, Z);
                vtx[0].Tu = 0;
                vtx[0].Tv = 1;
                vtx[1].Position = new Vector3(X2, Y, Z);
                vtx[1].Tu = 1;
                vtx[1].Tv = 0;
                vtx[2].Position = new Vector3(X, Y, Z);
                vtx[2].Tu = 0;
                vtx[2].Tv = 0;

                vtx[3].Position = new Vector3(X2, Y, Z);
                vtx[3].Tu = 1;
                vtx[3].Tv = 0;
                vtx[4].Position = new Vector3(X, Y2, Z);
                vtx[4].Tu = 0;
                vtx[4].Tv = 1;
                vtx[5].Position = new Vector3(X2, Y2, Z);
                vtx[5].Tu = 1;
                vtx[5].Tv = 1;

                vtx[6].Position = new Vector3(X, Y, Z2);
                vtx[6].Tu = 0;
                vtx[6].Tv = 0;
                vtx[7].Position = new Vector3(X2, Y, Z2);
                vtx[7].Tu = 1;
                vtx[7].Tv = 0;
                vtx[8].Position = new Vector3(X, Y2, Z2);
                vtx[8].Tu = 0;
                vtx[8].Tv = 1;

                vtx[9].Position = new Vector3(X2, Y2, Z2);
                vtx[9].Tu = 1;
                vtx[9].Tv = 1;
                vtx[10].Position = new Vector3(X, Y2, Z2);
                vtx[10].Tu = 0;
                vtx[10].Tv = 1;
                vtx[11].Position = new Vector3(X2, Y, Z2);
                vtx[11].Tu = 1;
                vtx[11].Tv = 0;

                vtx[12].Position = new Vector3(X, Y, Z);
                vtx[12].Tu = 0;
                vtx[12].Tv = 0;
                vtx[13].Position = new Vector3(X2, Y, Z);
                vtx[13].Tu = 1;
                vtx[13].Tv = 0;
                vtx[14].Position = new Vector3(X, Y, Z2);
                vtx[14].Tu = 0;
                vtx[14].Tv = 1;

                vtx[15].Position = new Vector3(X2, Y, Z2);
                vtx[15].Tu = 1;
                vtx[15].Tv = 1;
                vtx[16].Position = new Vector3(X, Y, Z2);
                vtx[16].Tu = 0;
                vtx[16].Tv = 1;
                vtx[17].Position = new Vector3(X2, Y, Z);
                vtx[17].Tu = 1;
                vtx[17].Tv = 0;

                vtx[18].Position = new Vector3(X, Y2, Z2);
                vtx[18].Tu = 0;
                vtx[18].Tv = 1;
                vtx[19].Position = new Vector3(X2, Y2, Z);
                vtx[19].Tu = 1;
                vtx[19].Tv = 0;
                vtx[20].Position = new Vector3(X, Y2, Z);
                vtx[20].Tu = 0;
                vtx[20].Tv = 0;

                vtx[21].Position = new Vector3(X2, Y2, Z);
                vtx[21].Tu = 1;
                vtx[21].Tv = 0;
                vtx[22].Position = new Vector3(X, Y2, Z2);
                vtx[22].Tu = 0;
                vtx[22].Tv = 1;
                vtx[23].Position = new Vector3(X2, Y2, Z2);
                vtx[23].Tu = 1;
                vtx[23].Tv = 1;

                vtx[24].Position = new Vector3(X, Y, Z2);
                vtx[24].Tu = 0;
                vtx[24].Tv = 1;
                vtx[25].Position = new Vector3(X, Y2, Z);
                vtx[25].Tu = 1;
                vtx[25].Tv = 0;
                vtx[26].Position = new Vector3(X, Y, Z);
                vtx[26].Tu = 0;
                vtx[26].Tv = 0;

                vtx[27].Position = new Vector3(X, Y2, Z);
                vtx[27].Tu = 1;
                vtx[27].Tv = 0;
                vtx[28].Position = new Vector3(X, Y, Z2);
                vtx[28].Tu = 0;
                vtx[28].Tv = 1;
                vtx[29].Position = new Vector3(X, Y2, Z2);
                vtx[29].Tu = 1;
                vtx[29].Tv = 1;

                vtx[30].Position = new Vector3(X2, Y, Z);
                vtx[30].Tu = 0;
                vtx[30].Tv = 0;
                vtx[31].Position = new Vector3(X2, Y2, Z);
                vtx[31].Tu = 1;
                vtx[31].Tv = 0;
                vtx[32].Position = new Vector3(X2, Y, Z2);
                vtx[32].Tu = 0;
                vtx[32].Tv = 1;

                vtx[33].Position = new Vector3(X2, Y2, Z2);
                vtx[33].Tu = 1;
                vtx[33].Tv = 1;
                vtx[34].Position = new Vector3(X2, Y, Z2);
                vtx[34].Tu = 0;
                vtx[34].Tv = 1;
                vtx[35].Position = new Vector3(X2, Y2, Z);
                vtx[35].Tu = 1;
                vtx[35].Tv = 0;

                //Normals
                vtx[0].Normal = new Vector3(0f, 1f, 0f);
                vtx[1].Normal = new Vector3(1f, 0f, 0f);
                vtx[2].Normal = new Vector3(0f, 0f, 0f);
                vtx[3].Normal = new Vector3(1f, 0f, 0f);
                vtx[4].Normal = new Vector3(0f, 1f, 0f);
                vtx[5].Normal = new Vector3(1f, 1f, 0f);
                vtx[6].Normal = new Vector3(0f, 0f, 1f);
                vtx[7].Normal = new Vector3(1f, 0f, 1f);
                vtx[8].Normal = new Vector3(0f, 1f, 1f);
                vtx[9].Normal = new Vector3(1f, 1f, 1f);
                vtx[10].Normal = new Vector3(0f, 1f, 1f);
                vtx[11].Normal = new Vector3(1f, 0f, 1f);
                vtx[12].Normal = new Vector3(0f, 0f, 0f);
                vtx[13].Normal = new Vector3(1f, 0f, 0f);
                vtx[14].Normal = new Vector3(0f, 0f, 1f);
                vtx[15].Normal = new Vector3(1f, 0f, 1f);
                vtx[16].Normal = new Vector3(0f, 0f, 1f);
                vtx[17].Normal = new Vector3(1f, 0f, 0f);
                vtx[18].Normal = new Vector3(0f, 1f, 1f);
                vtx[19].Normal = new Vector3(1f, 1f, 0f);
                vtx[20].Normal = new Vector3(0f, 1f, 0f);
                vtx[21].Normal = new Vector3(1f, 1f, 0f);
                vtx[22].Normal = new Vector3(0f, 1f, 1f);
                vtx[23].Normal = new Vector3(1f, 1f, 1f);
                vtx[24].Normal = new Vector3(0f, 0f, 1f);
                vtx[25].Normal = new Vector3(0f, 1f, 0f);
                vtx[26].Normal = new Vector3(0f, 0f, 0f);
                vtx[27].Normal = new Vector3(0f, 1f, 0f);
                vtx[28].Normal = new Vector3(0f, 0f, 1f);
                vtx[29].Normal = new Vector3(0f, 1f, 1f);
                vtx[30].Normal = new Vector3(1f, 0f, 0f);
                vtx[31].Normal = new Vector3(1f, 1f, 0f);
                vtx[32].Normal = new Vector3(1f, 0f, 1f);
                vtx[33].Normal = new Vector3(1f, 1f, 1f);
                vtx[34].Normal = new Vector3(1f, 0f, 1f);
                vtx[35].Normal = new Vector3(1f, 1f, 0f);
                //----
                //----
                device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, vtx.Length / 3, vtx);

                if (Collide)
                {
                    Collides.Add(new CollideObj(new Vector3(TransformPos.X + X, TransformPos.Y + Y, TransformPos.Z + Z), new Vector3(TransformPos.X + X2 - X, TransformPos.Y + Y2 - Y, TransformPos.Z + Z2 - Z)));
                    CollideName.Add("");
                }
                Triangles += 12;
                Points += 36;
            }
        }
        public void CubeUV(Vector3 Position, Vector3 To, Texture Tex, float U, float V)
        {
            if (Camera)
            {
                float X = Position.X, Y = Position.Y, Z = Position.Z, X2 = To.X, Y2 = To.Y, Z2 = To.Z;
                CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[36];
                device.Material = Material;
                device.SetTexture(0, Tex);
                //Draw
                vtx[0].Position = new Vector3(X, Y2, Z);
                vtx[0].Tu = 0;
                vtx[0].Tv = V;
                vtx[1].Position = new Vector3(X2, Y, Z);
                vtx[1].Tu = U;
                vtx[1].Tv = 0;
                vtx[2].Position = new Vector3(X, Y, Z);
                vtx[2].Tu = 0;
                vtx[2].Tv = 0;

                vtx[3].Position = new Vector3(X2, Y, Z);
                vtx[3].Tu = U;
                vtx[3].Tv = 0;
                vtx[4].Position = new Vector3(X, Y2, Z);
                vtx[4].Tu = 0;
                vtx[4].Tv = V;
                vtx[5].Position = new Vector3(X2, Y2, Z);
                vtx[5].Tu = U;
                vtx[5].Tv = V;

                vtx[6].Position = new Vector3(X, Y, Z2);
                vtx[6].Tu = 0;
                vtx[6].Tv = 0;
                vtx[7].Position = new Vector3(X2, Y, Z2);
                vtx[7].Tu = U;
                vtx[7].Tv = 0;
                vtx[8].Position = new Vector3(X, Y2, Z2);
                vtx[8].Tu = 0;
                vtx[8].Tv = V;

                vtx[9].Position = new Vector3(X2, Y2, Z2);
                vtx[9].Tu = U;
                vtx[9].Tv = V;
                vtx[10].Position = new Vector3(X, Y2, Z2);
                vtx[10].Tu = 0;
                vtx[10].Tv = V;
                vtx[11].Position = new Vector3(X2, Y, Z2);
                vtx[11].Tu = U;
                vtx[11].Tv = 0;

                vtx[12].Position = new Vector3(X, Y, Z);
                vtx[12].Tu = 0;
                vtx[12].Tv = 0;
                vtx[13].Position = new Vector3(X2, Y, Z);
                vtx[13].Tu = U;
                vtx[13].Tv = 0;
                vtx[14].Position = new Vector3(X, Y, Z2);
                vtx[14].Tu = 0;
                vtx[14].Tv = V;

                vtx[15].Position = new Vector3(X2, Y, Z2);
                vtx[15].Tu = U;
                vtx[15].Tv = V;
                vtx[16].Position = new Vector3(X, Y, Z2);
                vtx[16].Tu = 0;
                vtx[16].Tv = V;
                vtx[17].Position = new Vector3(X2, Y, Z);
                vtx[17].Tu = U;
                vtx[17].Tv = 0;

                vtx[18].Position = new Vector3(X, Y2, Z2);
                vtx[18].Tu = 0;
                vtx[18].Tv = V;
                vtx[19].Position = new Vector3(X2, Y2, Z);
                vtx[19].Tu = U;
                vtx[19].Tv = 0;
                vtx[20].Position = new Vector3(X, Y2, Z);
                vtx[20].Tu = 0;
                vtx[20].Tv = 0;

                vtx[21].Position = new Vector3(X2, Y2, Z);
                vtx[21].Tu = U;
                vtx[21].Tv = 0;
                vtx[22].Position = new Vector3(X, Y2, Z2);
                vtx[22].Tu = 0;
                vtx[22].Tv = V;
                vtx[23].Position = new Vector3(X2, Y2, Z2);
                vtx[23].Tu = U;
                vtx[23].Tv = V;

                vtx[24].Position = new Vector3(X, Y, Z2);
                vtx[24].Tu = 0;
                vtx[24].Tv = V;
                vtx[25].Position = new Vector3(X, Y2, Z);
                vtx[25].Tu = U;
                vtx[25].Tv = 0;
                vtx[26].Position = new Vector3(X, Y, Z);
                vtx[26].Tu = 0;
                vtx[26].Tv = 0;

                vtx[27].Position = new Vector3(X, Y2, Z);
                vtx[27].Tu = U;
                vtx[27].Tv = 0;
                vtx[28].Position = new Vector3(X, Y, Z2);
                vtx[28].Tu = 0;
                vtx[28].Tv = V;
                vtx[29].Position = new Vector3(X, Y2, Z2);
                vtx[29].Tu = U;
                vtx[29].Tv = V;

                vtx[30].Position = new Vector3(X2, Y, Z);
                vtx[30].Tu = 0;
                vtx[30].Tv = 0;
                vtx[31].Position = new Vector3(X2, Y2, Z);
                vtx[31].Tu = U;
                vtx[31].Tv = 0;
                vtx[32].Position = new Vector3(X2, Y, Z2);
                vtx[32].Tu = 0;
                vtx[32].Tv = V;

                vtx[33].Position = new Vector3(X2, Y2, Z2);
                vtx[33].Tu = U;
                vtx[33].Tv = V;
                vtx[34].Position = new Vector3(X2, Y, Z2);
                vtx[34].Tu = 0;
                vtx[34].Tv = V;
                vtx[35].Position = new Vector3(X2, Y2, Z);
                vtx[35].Tu = U;
                vtx[35].Tv = 0;

                //Normals
                vtx[0].Normal = new Vector3(0f, 1f, 0f);
                vtx[1].Normal = new Vector3(1f, 0f, 0f);
                vtx[2].Normal = new Vector3(0f, 0f, 0f);
                vtx[3].Normal = new Vector3(1f, 0f, 0f);
                vtx[4].Normal = new Vector3(0f, 1f, 0f);
                vtx[5].Normal = new Vector3(1f, 1f, 0f);
                vtx[6].Normal = new Vector3(0f, 0f, 1f);
                vtx[7].Normal = new Vector3(1f, 0f, 1f);
                vtx[8].Normal = new Vector3(0f, 1f, 1f);
                vtx[9].Normal = new Vector3(1f, 1f, 1f);
                vtx[10].Normal = new Vector3(0f, 1f, 1f);
                vtx[11].Normal = new Vector3(1f, 0f, 1f);
                vtx[12].Normal = new Vector3(0f, 0f, 0f);
                vtx[13].Normal = new Vector3(1f, 0f, 0f);
                vtx[14].Normal = new Vector3(0f, 0f, 1f);
                vtx[15].Normal = new Vector3(1f, 0f, 1f);
                vtx[16].Normal = new Vector3(0f, 0f, 1f);
                vtx[17].Normal = new Vector3(1f, 0f, 0f);
                vtx[18].Normal = new Vector3(0f, 1f, 1f);
                vtx[19].Normal = new Vector3(1f, 1f, 0f);
                vtx[20].Normal = new Vector3(0f, 1f, 0f);
                vtx[21].Normal = new Vector3(1f, 1f, 0f);
                vtx[22].Normal = new Vector3(0f, 1f, 1f);
                vtx[23].Normal = new Vector3(1f, 1f, 1f);
                vtx[24].Normal = new Vector3(0f, 0f, 1f);
                vtx[25].Normal = new Vector3(0f, 1f, 0f);
                vtx[26].Normal = new Vector3(0f, 0f, 0f);
                vtx[27].Normal = new Vector3(0f, 1f, 0f);
                vtx[28].Normal = new Vector3(0f, 0f, 1f);
                vtx[29].Normal = new Vector3(0f, 1f, 1f);
                vtx[30].Normal = new Vector3(1f, 0f, 0f);
                vtx[31].Normal = new Vector3(1f, 1f, 0f);
                vtx[32].Normal = new Vector3(1f, 0f, 1f);
                vtx[33].Normal = new Vector3(1f, 1f, 1f);
                vtx[34].Normal = new Vector3(1f, 0f, 1f);
                vtx[35].Normal = new Vector3(1f, 1f, 0f);
                //----
                //----
                device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, vtx.Length / 3, vtx);

                if (Collide)
                {
                    Collides.Add(new CollideObj(new Vector3(TransformPos.X + X, TransformPos.Y + Y, TransformPos.Z + Z), new Vector3(TransformPos.X + X2 - X, TransformPos.Y + Y2 - Y, TransformPos.Z + Z2 - Z)));
                    CollideName.Add("");
                }
                Triangles += 12;
                Points += 36;
            }
        }
        public void CubeTex(Vector3 Position, Vector3 To, Texture[] Tex, Vector2[] UV)
        {
            if (Camera && Tex.Length == 6 && UV.Length == 6)
            {
                float X = Position.X, Y = Position.Y, Z = Position.Z, X2 = To.X, Y2 = To.Y, Z2 = To.Z;
                CustomVertex.PositionNormalTextured[] vtx0 = new CustomVertex.PositionNormalTextured[6];
                CustomVertex.PositionNormalTextured[] vtx1 = new CustomVertex.PositionNormalTextured[6];
                CustomVertex.PositionNormalTextured[] vtx2 = new CustomVertex.PositionNormalTextured[6];
                CustomVertex.PositionNormalTextured[] vtx3 = new CustomVertex.PositionNormalTextured[6];
                CustomVertex.PositionNormalTextured[] vtx4 = new CustomVertex.PositionNormalTextured[6];
                CustomVertex.PositionNormalTextured[] vtx5 = new CustomVertex.PositionNormalTextured[6];
                device.Material = Material;
                //Draw
                vtx0[0].Position = new Vector3(X, Y2, Z);
                vtx0[0].Tu = 0;
                vtx0[0].Tv = UV[0].Y;
                vtx0[1].Position = new Vector3(X2, Y, Z);
                vtx0[1].Tu = UV[0].X;
                vtx0[1].Tv = 0;
                vtx0[2].Position = new Vector3(X, Y, Z);
                vtx0[2].Tu = 0;
                vtx0[2].Tv = 0;

                vtx0[3].Position = new Vector3(X2, Y, Z);
                vtx0[3].Tu = UV[0].X;
                vtx0[3].Tv = 0;
                vtx0[4].Position = new Vector3(X, Y2, Z);
                vtx0[4].Tu = 0;
                vtx0[4].Tv = UV[0].Y;
                vtx0[5].Position = new Vector3(X2, Y2, Z);
                vtx0[5].Tu = UV[0].X;
                vtx0[5].Tv = UV[0].Y;

                vtx1[0].Position = new Vector3(X, Y, Z2);
                vtx1[0].Tu = 0;
                vtx1[0].Tv = 0;
                vtx1[1].Position = new Vector3(X2, Y, Z2);
                vtx1[1].Tu = UV[1].X;
                vtx1[1].Tv = 0;
                vtx1[2].Position = new Vector3(X, Y2, Z2);
                vtx1[2].Tu = 0;
                vtx1[2].Tv = UV[1].Y;

                vtx1[3].Position = new Vector3(X2, Y2, Z2);
                vtx1[3].Tu = UV[1].X;
                vtx1[3].Tv = UV[1].Y;
                vtx1[4].Position = new Vector3(X, Y2, Z2);
                vtx1[4].Tu = 0;
                vtx1[4].Tv = UV[1].Y;
                vtx1[5].Position = new Vector3(X2, Y, Z2);
                vtx1[5].Tu = UV[1].X;
                vtx1[5].Tv = 0;

                vtx2[0].Position = new Vector3(X, Y, Z);
                vtx2[0].Tu = 0;
                vtx2[0].Tv = 0;
                vtx2[1].Position = new Vector3(X2, Y, Z);
                vtx2[1].Tu = UV[2].X;
                vtx2[1].Tv = 0;
                vtx2[2].Position = new Vector3(X, Y, Z2);
                vtx2[2].Tu = 0;
                vtx2[2].Tv = UV[2].Y;

                vtx2[3].Position = new Vector3(X2, Y, Z2);
                vtx2[3].Tu = UV[2].X;
                vtx2[3].Tv = UV[2].Y;
                vtx2[4].Position = new Vector3(X, Y, Z2);
                vtx2[4].Tu = 0;
                vtx2[4].Tv = UV[2].Y;
                vtx2[5].Position = new Vector3(X2, Y, Z);
                vtx2[5].Tu = UV[2].X;
                vtx2[5].Tv = 0;

                vtx3[0].Position = new Vector3(X, Y2, Z2);
                vtx3[0].Tu = 0;
                vtx3[0].Tv = UV[3].Y;
                vtx3[1].Position = new Vector3(X2, Y2, Z);
                vtx3[1].Tu = UV[3].X;
                vtx3[1].Tv = 0;
                vtx3[2].Position = new Vector3(X, Y2, Z);
                vtx3[2].Tu = 0;
                vtx3[2].Tv = 0;

                vtx3[3].Position = new Vector3(X2, Y2, Z);
                vtx3[3].Tu = UV[3].X;
                vtx3[3].Tv = 0;
                vtx3[4].Position = new Vector3(X, Y2, Z2);
                vtx3[4].Tu = 0;
                vtx3[4].Tv = UV[3].Y;
                vtx3[5].Position = new Vector3(X2, Y2, Z2);
                vtx3[5].Tu = UV[3].X;
                vtx3[5].Tv = UV[3].Y;

                vtx4[0].Position = new Vector3(X, Y, Z2);
                vtx4[0].Tu = 0;
                vtx4[0].Tv = UV[4].Y;
                vtx4[1].Position = new Vector3(X, Y2, Z);
                vtx4[1].Tu = UV[4].X;
                vtx4[1].Tv = 0;
                vtx4[2].Position = new Vector3(X, Y, Z);
                vtx4[2].Tu = 0;
                vtx4[2].Tv = 0;

                vtx4[3].Position = new Vector3(X, Y2, Z);
                vtx4[3].Tu = UV[4].X;
                vtx4[3].Tv = 0;
                vtx4[4].Position = new Vector3(X, Y, Z2);
                vtx4[4].Tu = 0;
                vtx4[4].Tv = UV[4].Y;
                vtx4[5].Position = new Vector3(X, Y2, Z2);
                vtx4[5].Tu = UV[4].X;
                vtx4[5].Tv = UV[4].Y;

                vtx5[0].Position = new Vector3(X2, Y, Z);
                vtx5[0].Tu = 0;
                vtx5[0].Tv = 0;
                vtx5[1].Position = new Vector3(X2, Y2, Z);
                vtx5[1].Tu = UV[5].X;
                vtx5[1].Tv = 0;
                vtx5[2].Position = new Vector3(X2, Y, Z2);
                vtx5[2].Tu = 0;
                vtx5[2].Tv = UV[5].Y;

                vtx5[3].Position = new Vector3(X2, Y2, Z2);
                vtx5[3].Tu = UV[5].X;
                vtx5[3].Tv = UV[5].Y;
                vtx5[4].Position = new Vector3(X2, Y, Z2);
                vtx5[4].Tu = 0;
                vtx5[4].Tv = UV[5].Y;
                vtx5[5].Position = new Vector3(X2, Y2, Z);
                vtx5[5].Tu = UV[5].X;
                vtx5[5].Tv = 0;

                //Normals
                vtx0[0].Normal = new Vector3(0f, 1f, 0f);
                vtx0[1].Normal = new Vector3(1f, 0f, 0f);
                vtx0[2].Normal = new Vector3(0f, 0f, 0f);
                vtx0[3].Normal = new Vector3(1f, 0f, 0f);
                vtx0[4].Normal = new Vector3(0f, 1f, 0f);
                vtx0[5].Normal = new Vector3(1f, 1f, 0f);
                vtx1[0].Normal = new Vector3(0f, 0f, 1f);
                vtx1[1].Normal = new Vector3(1f, 0f, 1f);
                vtx1[2].Normal = new Vector3(0f, 1f, 1f);
                vtx1[3].Normal = new Vector3(1f, 1f, 1f);
                vtx1[4].Normal = new Vector3(0f, 1f, 1f);
                vtx1[5].Normal = new Vector3(1f, 0f, 1f);
                vtx2[0].Normal = new Vector3(0f, 0f, 0f);
                vtx2[1].Normal = new Vector3(1f, 0f, 0f);
                vtx2[2].Normal = new Vector3(0f, 0f, 1f);
                vtx2[3].Normal = new Vector3(1f, 0f, 1f);
                vtx2[4].Normal = new Vector3(0f, 0f, 1f);
                vtx2[5].Normal = new Vector3(1f, 0f, 0f);
                vtx3[0].Normal = new Vector3(0f, 1f, 1f);
                vtx3[1].Normal = new Vector3(1f, 1f, 0f);
                vtx3[2].Normal = new Vector3(0f, 1f, 0f);
                vtx3[3].Normal = new Vector3(1f, 1f, 0f);
                vtx3[4].Normal = new Vector3(0f, 1f, 1f);
                vtx3[5].Normal = new Vector3(1f, 1f, 1f);
                vtx4[0].Normal = new Vector3(0f, 0f, 1f);
                vtx4[1].Normal = new Vector3(0f, 1f, 0f);
                vtx4[2].Normal = new Vector3(0f, 0f, 0f);
                vtx4[3].Normal = new Vector3(0f, 1f, 0f);
                vtx4[4].Normal = new Vector3(0f, 0f, 1f);
                vtx4[5].Normal = new Vector3(0f, 1f, 1f);
                vtx5[0].Normal = new Vector3(1f, 0f, 0f);
                vtx5[1].Normal = new Vector3(1f, 1f, 0f);
                vtx5[2].Normal = new Vector3(1f, 0f, 1f);
                vtx5[3].Normal = new Vector3(1f, 1f, 1f);
                vtx5[4].Normal = new Vector3(1f, 0f, 1f);
                vtx5[5].Normal = new Vector3(1f, 1f, 0f);
                //----
                //----
                device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                if (Tex[0] != null) { device.SetTexture(0, Tex[0]); device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, vtx0.Length / 3, vtx0); }
                if (Tex[1] != null) { device.SetTexture(0, Tex[1]); device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, vtx1.Length / 3, vtx1); }
                if (Tex[2] != null) { device.SetTexture(0, Tex[2]); device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, vtx2.Length / 3, vtx2); }
                if (Tex[3] != null) { device.SetTexture(0, Tex[3]); device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, vtx3.Length / 3, vtx3); }
                if (Tex[4] != null) { device.SetTexture(0, Tex[4]); device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, vtx4.Length / 3, vtx4); }
                if (Tex[5] != null) { device.SetTexture(0, Tex[5]); device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, vtx5.Length / 3, vtx5); }

                if (Collide)
                {
                    Collides.Add(new CollideObj(new Vector3(TransformPos.X + X, TransformPos.Y + Y, TransformPos.Z + Z), new Vector3(TransformPos.X + X2 - X, TransformPos.Y + Y2 - Y, TransformPos.Z + Z2 - Z)));
                    CollideName.Add("");
                }
                Triangles += 12;
                Points += 36;
            }
        }
        public void Ellipsoid(Vector3 Position, float Radius, Texture Tex)
        {
            float X = Position.X, Y = Position.Y, Z = Position.Z;
            Mesh msh = Mesh.Sphere(device, Radius, 24, 24);
            device.Material = Material;
            NormalMat = device.Transform.World;
            device.Transform.World = Matrix.Translation(new Vector3(X + Radius / 2, Y + Radius / 2, Z + Radius / 2));
            msh.ComputeNormals();
            msh.DrawSubset(0);
            device.Transform.World = NormalMat;
        }
        public void Cylinder(Vector3 Position, Vector3 RadLen, Texture Tex)
        {
            float X = Position.X, Y = Position.Y, Z = Position.Z;
            Mesh msh = Mesh.Cylinder(device, RadLen.X, RadLen.Y, RadLen.Z, 10, 10);
            device.Material = Material;
            device.SetTexture(0, Tex);
            NormalMat = device.Transform.World;
            device.Transform.World = Matrix.Translation(new Vector3(X, Y, Z));
            msh.ComputeNormals();
            msh.DrawSubset(0);
            device.Transform.World = NormalMat;
        }
        public void Pyramid(Vector3 Position, Vector3 To, Texture Tex)
        {
            float X = Position.X, Y = Position.Y, Z = Position.Z, X2 = To.X, Y2 = To.Y, Z2 = To.Z;
            CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[12];
            //Draw
            vtx[0].Position = new Vector3(X, Y, Z);
            vtx[0].Tu = 0;
            vtx[0].Tv = 0;
            vtx[1].Position = new Vector3(X2, Y, Z);
            vtx[1].Tu = 1;
            vtx[1].Tv = 0;
            vtx[2].Position = new Vector3(X + (X2 - X) / 2, Y + (Y2 - Y) / 2, Z2);
            vtx[2].Tu = 0.5f;
            vtx[2].Tv = 1;

            vtx[3].Position = new Vector3(X2, Y, Z);
            vtx[3].Tu = 0;
            vtx[3].Tv = 0;
            vtx[4].Position = new Vector3(X2, Y2, Z);
            vtx[4].Tu = 1;
            vtx[4].Tv = 0;
            vtx[5].Position = new Vector3(X + (X2 - X) / 2, Y + (Y2 - Y) / 2, Z2);
            vtx[5].Tu = 1;
            vtx[5].Tv = 1;

            vtx[6].Position = new Vector3(X2, Y2, Z);
            vtx[6].Tu = 0;
            vtx[6].Tv = 0;
            vtx[7].Position = new Vector3(X, Y2, Z);
            vtx[7].Tu = 1;
            vtx[7].Tv = 0;
            vtx[8].Position = new Vector3(X + (X2 - X) / 2, Y + (Y2 - Y) / 2, Z2);
            vtx[8].Tu = 1;
            vtx[8].Tv = 1;

            vtx[9].Position = new Vector3(X, Y2, Z);
            vtx[9].Tu = 0;
            vtx[9].Tv = 0;
            vtx[10].Position = new Vector3(X, Y, Z);
            vtx[10].Tu = 1;
            vtx[10].Tv = 0;
            vtx[11].Position = new Vector3(X + (X2 - X) / 2, Y + (Y2 - Y) / 2, Z2);
            vtx[11].Tu = 1;
            vtx[11].Tv = 1;

            vtx[0].Normal = new Vector3(0f, 0f, 0f);
            vtx[1].Normal = new Vector3(1f, 0f, 0f);
            vtx[2].Normal = new Vector3(0.5f, 0.5f, 1f);
            vtx[3].Normal = new Vector3(1f, 0f, 0f);
            vtx[4].Normal = new Vector3(1f, 1f, 0f);
            vtx[5].Normal = new Vector3(0.5f, 0.5f, 1f);
            vtx[6].Normal = new Vector3(1f, 1f, 0f);
            vtx[7].Normal = new Vector3(0f, 1f, 0f);
            vtx[8].Normal = new Vector3(0.5f, 0.5f, 1f);
            vtx[9].Normal = new Vector3(0f, 1f, 0f);
            vtx[10].Normal = new Vector3(0f, 0f, 0f);
            vtx[11].Normal = new Vector3(0.5f, 0.5f, 1f);
            //----
            device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
            device.SetTexture(0, Tex);
            device.Material = Material;
            device.DrawUserPrimitives(PrimitiveType.TriangleList, 4, vtx);

            Triangles += 4;
            Points += 12;
        }
        public float Direction(float Dir)
        {
            return Dir / 60;
        }
        public float Angle(float Ang)
        {
            return Ang * 60;
        }
        public Vector3 Lengthdir(float Dir, float Points)
        {
            return new Vector3((float)Math.Cos(Dir), (float)Math.Sin(Dir), (float)Math.Tan(Dir)) * Points;
        }
        public float PointDirection(Vector2 From, Vector2 To)
        {
            return (float)Math.Atan2(To.Y - From.Y, To.X - From.X) * 180f / (float)Math.PI;
        }
        public float PointDistance(Vector3 From, Vector3 To, string Vars="XYZ")
        {
            float Ans = 0;
            if (Vars.Contains("X")) { Ans += (From.X - To.X); }
            if (Vars.Contains("Y")) { Ans += (From.Y - To.Y); }
            if (Vars.Contains("Z")) { Ans += (From.Z - To.Z); }
            //return (From.X - To.X) * (From.X - To.X) + (From.Y - To.Y) * (From.Y - To.Y) + (From.Z - To.Z) * (From.Z - To.Z);
            return Math.Abs(Ans);
        }
        public void TransformBegin()
        {
            Transform = true;
            NormalMat = device.Transform.World;
        }
        public void TransformByObj(Obj Object)
        {
            Transform = true;
            NormalMat = device.Transform.World;
            TransformPos = Object.Position + Object.Pivot;
            //if (Collide) { AddCollide(Object.Position, Object.Size); }
            NormalCollide = Collide;
            Collide = false;
            device.Transform.World = Matrix.Translation(Object.Position + Object.Pivot) * Matrix.RotationX(Object.Rotation.X) * Matrix.RotationY(Object.Rotation.Y) * Matrix.RotationZ(Object.Rotation.Z);
        }
        public void TransformRotX(float Angle)
        {
            device.Transform.World *= Matrix.RotationX(Angle);
        }
        public void TransformRotY(float Angle)
        {
            device.Transform.World *= Matrix.RotationY(Angle);
        }
        public void TransformRotZ(float Angle)
        {
            device.Transform.World *= Matrix.RotationZ(Angle);
        }
        public void TransformTranslate(Vector3 Position)
        {
            TransformPos = Position;
            device.Transform.World *= Matrix.Translation(Position);
        }
        public void TransformEnd()
        {
            if (Transform)
            {
                TransformPos = new Vector3(0f, 0f, 0f);
                device.Transform.World = NormalMat;
                Collide = NormalCollide;
                Transform = false;
            }
        }
        public void DrawMesh(string Name, Vector3 Position)
        {
            if (!CheckPosition(Position, CamAt - new Vector3(FarRender, FarRender, FarRender), CamAt + new Vector3(FarRender, FarRender, FarRender))) { return; }
            Mesh msh = Meshes.Get(Name);
            string type = Meshes.GetType(Name);
            if (type == ".x")
            {
                device.RenderState.CullMode = Cull.CounterClockwise;
                if (!Transform)
                {
                    NormalMat = device.Transform.World;
                    device.Transform.World = Matrix.Translation(Position);
                }
                /*for (int i = 0; i < Meshes.GetLength(Variable.MATERIAL_LENGTH, Name); i++)
                {
                    if (Meshes.HaveMaterial(Name))
                    {
                        device.Material = Meshes.GetMaterial(Name, i);
                        device.SetTexture(0, Meshes.GetTexture(Name, i));
                    }
                    msh.DrawSubset(i);
                }*/
                msh.DrawSubset(0);
                if (!Transform) { device.Transform.World = NormalMat; }
                device.RenderState.CullMode = Culling;
            }
            else if (type == ".dmf")
            {
                if (!Transform)
                {
                    NormalMat = device.Transform.World;
                    device.Transform.World = Matrix.Translation(Position);
                    TransformPos = Position;
                }
                string[] scr = Meshes.GetLineInner(Name);
                string sc, func;
                List<CustomVertex.PositionNormalTextured[]> vtx = new List<CustomVertex.PositionNormalTextured[]>();
                List<int> vtxt = new List<int>();
                List<bool> vtxc = new List<bool>();
                List<Texture> vtxtex = new List<Texture>();
                List<CustomVertex.PositionNormalTextured> cvtx = new List<CustomVertex.PositionNormalTextured>();
                /* Funcs
                 * l = Primitive List
                 * f = Primitive Fan
                 * tx = Texture
                ----------*/
                for (int i = 0; i < scr.Length; i++)
                {
                    sc = scr[i];
                    func=sc.Substring(0, 1);
                    if (func == "l")
                    {
                        if (cvtx.Count > 0)
                        {
                            vtx.Add(cvtx.ToArray());
                            cvtx.Clear();
                        }
                        vtxt.Add(0);
                        vtxc.Add(true);
                        vtxtex.Add(null);
                    }
                    else if (func == "f")
                    {
                        if (cvtx.Count > 0)
                        {
                            vtx.Add(cvtx.ToArray());
                            cvtx.Clear();
                        }
                        vtxt.Add(1);
                        vtxc.Add(true);
                        vtxtex.Add(null);
                    }
                    else if (func == "t")
                    {
                        string[] args = sc.Split(' ');
                        vtxtex[vtxt.Count - 1] = Textures.Get(args[1]);
                    }
                    else if (func == "b")
                    {
                        string[] args = sc.Split(' ');
                        if (args.Length == 7)
                        {
                            float X, Y, Z, X2, Y2, Z2;
                            float.TryParse(args[1], out X);
                            float.TryParse(args[2], out Y);
                            float.TryParse(args[3], out Z);
                            float.TryParse(args[4], out X2);
                            float.TryParse(args[5], out Y2);
                            float.TryParse(args[6], out Z2);
                            AddRealCollideID(new Vector3(TransformPos.X + X, TransformPos.Y + Y, TransformPos.Z + Z), new Vector3(X2 - X, Y2 - Y, Z2 - Z));
                        }
                    }
                    else if (func == "c")
                    {
                        vtxc[vtxt.Count - 1] = false;
                    }
                    else
                    {
                        string[] args = sc.Split(' ');
                        if (args.Length == 8)
                        {
                            float X, Y, Z, Tu, Tv, NX, NY, NZ;
                            float.TryParse(args[0], out X);
                            float.TryParse(args[1], out Y);
                            float.TryParse(args[2], out Z);
                            float.TryParse(args[3], out Tu);
                            float.TryParse(args[4], out Tv);
                            float.TryParse(args[5], out NX);
                            float.TryParse(args[6], out NY);
                            float.TryParse(args[7], out NZ);
                            CustomVertex.PositionNormalTextured vt = new CustomVertex.PositionNormalTextured(new Vector3(X, Y, Z), new Vector3(NX, NY, NZ), Tu, Tv);
                            cvtx.Add(vt);
                        }
                    }
                }
                if (cvtx.Count > 0)
                {
                    vtx.Add(cvtx.ToArray());
                    cvtx.Clear();
                }
                for (int i = 0; i < vtx.Count; i++)
                {
                    CustomVertex.PositionNormalTextured[] rvtx = vtx[i];
                    device.SetTexture(0, vtxtex[i]);
                    if (vtxc[i] == true) { device.RenderState.CullMode = Cull.Clockwise; }
                    else { device.RenderState.CullMode = Cull.None; }
                    if (vtxt[i] == 0) { device.DrawUserPrimitives(PrimitiveType.TriangleList, rvtx.Length / 3, rvtx); }
                    else { device.DrawUserPrimitives(PrimitiveType.TriangleFan, rvtx.Length, rvtx); }
                }
                device.RenderState.CullMode = Culling;
                device.Transform.World = NormalMat;
                if (!Transform) { TransformPos = new Vector3(0f, 0f, 0f); }
            }
            else if (type == ".obj")
            {
                string[] scr = Meshes.GetLineInner(Name);
                string sc, func, args;
                string[] arg;
                List<Vector3> vert = new List<Vector3>();
                /*Vector3[,] indice1 = new Vector3[100, 10000];
                Vector3[,] indice2 = new Vector3[100, 10000];
                Vector3[,] indice3 = new Vector3[100, 10000];*/
                List<Vector3[]> indice1 = new List<Vector3[]>();
                List<Vector3[]> indice2 = new List<Vector3[]>();
                List<Vector3[]> indice3 = new List<Vector3[]>();
                List<Vector3> cindice1 = new List<Vector3>();
                List<Vector3> cindice2 = new List<Vector3>();
                List<Vector3> cindice3 = new List<Vector3>();
                List<Vector2> coordinate = new List<Vector2>();
                List<Vector3> normal = new List<Vector3>();
                List<Material> material = new List<Material>();
                List<string> materialname = new List<string>();
                List<Texture> materialtexture = new List<Texture>();
                List<string> group = new List<string>();
                /*Material[] groupmtl = new Material[100];
                Texture[] grouptx = new Texture[100];*/
                List<Material> groupmtl = new List<Material>();
                List<Texture> grouptx = new List<Texture>();
                int mats = 0;
                int[] inds = new int[100];
                if (Meshes.HaveMaterial(Name))
                {
                    material = Meshes.GetMaterials(Name).ToList<Material>();
                    materialname = Meshes.GetMaterialNames(Name).ToList<string>();
                    materialtexture = Meshes.GetTextures(Name).ToList<Texture>();
                    mats = material.Count;
                }
                for (int i = 0; i < scr.Length; i++)
                {
                    if (scr[i] != "" || scr[i] != String.Empty)
                    {
                        sc = scr[i];
                        if (sc.Contains(" "))
                        {
                            func = sc.Substring(0, sc.IndexOf(" "));
                            args = sc.Substring(sc.IndexOf(" ") + 1, sc.Length - sc.IndexOf(" ") - 1);
                            arg = args.Split(' ');
                        }
                        else
                        {
                            func = "";
                            args = "";
                            arg = null;
                        }
                        if (func == "v")
                        {
                            for (int ii = 0; ii < 3; ii++) { arg[ii] = arg[ii].Replace(".", ","); }
                            float X, Y, Z;
                            float.TryParse(arg[0], out X);
                            float.TryParse(arg[1], out Y);
                            float.TryParse(arg[2], out Z);
                            vert.Add(new Vector3(X, Y, Z));
                        }
                        else if (func == "vt")
                        {
                            for (int ii = 0; ii < 2; ii++) { arg[ii] = arg[ii].Replace(".", ","); }
                            float U, V;
                            float.TryParse(arg[0], out U);
                            float.TryParse(arg[1], out V);
                            coordinate.Add(new Vector2(U, V));
                        }
                        else if (func == "vn")
                        {
                            for (int ii = 0; ii < 3; ii++) { arg[ii] = arg[ii].Replace(".", ","); }
                            float X, Y, Z;
                            float.TryParse(arg[0], out X);
                            float.TryParse(arg[1], out Y);
                            float.TryParse(arg[2], out Z);
                            normal.Add(new Vector3(X, Y, Z));
                        }
                        else if (func == "f")
                        {
                            string[] arg1 = arg[0].Split('/');
                            string[] arg2 = arg[1].Split('/');
                            string[] arg3 = arg[2].Split('/');
                            int X, Y, Z, X2, Y2, Z2, X3, Y3, Z3;
                            int.TryParse(arg1[0], out X);
                            int.TryParse(arg1[1], out Y);
                            int.TryParse(arg1[2], out Z);
                            int.TryParse(arg2[0], out X2);
                            int.TryParse(arg2[1], out Y2);
                            int.TryParse(arg2[2], out Z2);
                            int.TryParse(arg3[0], out X3);
                            int.TryParse(arg3[1], out Y3);
                            int.TryParse(arg3[2], out Z3);
                            cindice1.Add(new Vector3(X, Y, Z));
                            cindice2.Add(new Vector3(X2, Y2, Z2));
                            cindice3.Add(new Vector3(X3, Y3, Z3));
                            /*indice1[group.Count - 1, inds[group.Count - 1]] = new Vector3(X, Y, Z);
                            indice2[group.Count - 1, inds[group.Count - 1]] = new Vector3(X2, Y2, Z2);
                            indice3[group.Count - 1, inds[group.Count - 1]] = new Vector3(X3, Y3, Z3);*/
                            //inds[group.Count - 1]++;
                        }
                        else if (func == "g")
                        {
                            string nm;
                            nm = arg[0];
                            group.Add(nm);
                            Material mat = new Material();
                            mat.Ambient = Color.White;
                            mat.Diffuse = Color.White;
                            groupmtl.Add(mat);
                            grouptx.Add(null);
                            if (group.Count - 1 > 0)
                            {
                                indice1.Add(cindice1.ToArray());
                                indice2.Add(cindice2.ToArray());
                                indice3.Add(cindice3.ToArray());
                            }
                        }
                        else if (func == "usemtl")
                        {
                            string nm;
                            nm = arg[0];
                            Material mat = new Material();
                            Texture tx;
                            int id = -1;
                            for (int o = 0; o < mats; o++)
                            {
                                if (materialname[o] == nm)
                                {
                                    id = o;
                                }
                            }
                            if (id != -1)
                            {
                                mat = material[id];
                                tx = materialtexture[id];
                            }
                            else
                            {
                                mat = new Material();
                                tx = null;
                            }
                            groupmtl[group.Count - 1] = mat;
                            grouptx[group.Count - 1] = tx;
                        }
                        else if (func == "mtllib"&&!Meshes.HaveMaterial(Name))
                        {
                            string lib;
                            lib = arg[0];
                            if (File.Exists(ModelFolder + lib))
                            {
                                Material mt = new Material();
                                Texture tx = null;
                                string mtn = "mat" + material.Count.ToString();
                                mt.Diffuse = Color.White;
                                mt.Ambient = Color.White;
                                mt.Specular = Color.White;
                                string[] mscr = File.ReadAllLines(ModelFolder + lib);
                                string msc, mfunc, margs;
                                string[] marg;
                                for (int o = 0; o < mscr.Length; o++)
                                {
                                    msc = mscr[o];
                                    if (msc.Contains(" "))
                                    {
                                        mfunc = msc.Substring(0, msc.IndexOf(" "));
                                        margs = msc.Substring(msc.IndexOf(" ") + 1, msc.Length - msc.IndexOf(" ") - 1);
                                        marg = margs.Split(' ');
                                    }
                                    else
                                    {
                                        mfunc = "";
                                        margs = "";
                                        marg = null;
                                    }
                                    if (mfunc == "newmtl")
                                    {
                                        string nm;
                                        nm = marg[0];
                                        mtn = nm;
                                    }
                                    else if (mfunc == "Ka")
                                    {
                                        for (int ii = 0; ii < 3; ii++) { marg[ii] = marg[ii].Replace(".", ","); }
                                        float R, G, B;
                                        float.TryParse(marg[0], out R);
                                        float.TryParse(marg[1], out G);
                                        float.TryParse(marg[2], out B);
                                        /*R *= 100;
                                        G *= 100;
                                        B *= 100;*/
                                        Color cl = Color.FromArgb((int)(255 * R), (int)(255 * G), (int)(255 * B));
                                        mt.Ambient = cl;
                                    }
                                    else if (mfunc == "Kd")
                                    {
                                        for (int ii = 0; ii < 3; ii++) { marg[ii] = marg[ii].Replace(".", ","); }
                                        float R, G, B;
                                        float.TryParse(marg[0], out R);
                                        float.TryParse(marg[1], out G);
                                        float.TryParse(marg[2], out B);
                                        /*R *= 100;
                                        G *= 100;
                                        B *= 100;*/
                                        Color cl = Color.FromArgb((int)(255 * R), (int)(255 * G), (int)(255 * B));
                                        mt.Diffuse = cl;
                                    }
                                    else if (mfunc == "Ks")
                                    {
                                        for (int ii = 0; ii < 3; ii++) { marg[ii] = marg[ii].Replace(".", ","); }
                                        float R, G, B;
                                        float.TryParse(marg[0], out R);
                                        float.TryParse(marg[1], out G);
                                        float.TryParse(marg[2], out B);
                                        /*R *= 100;
                                        G *= 100;
                                        B *= 100;*/
                                        Color cl = Color.FromArgb((int)(255 * R), (int)(255 * G), (int)(255 * B));
                                        mt.Specular = cl;
                                    }
                                    else if (mfunc == "map_Kd")
                                    {
                                        string tex;
                                        tex = marg[0];
                                        tx=Textures.Get(tex);
                                    }
                                }
                                material.Add(mt);
                                materialname.Add(mtn);
                                materialtexture.Add(tx);
                                mats = material.Count;
                            }
                        }
                    }
                }
                if (!Meshes.HaveMaterial(Name))
                {
                    Meshes.SetMaterial(Name, material.ToArray());
                    Meshes.SetMaterialName(Name, materialname.ToArray());
                    Meshes.SetTexture(Name, materialtexture.ToArray());
                }
                indice1.Add(cindice1.ToArray());
                indice2.Add(cindice2.ToArray());
                indice3.Add(cindice3.ToArray());
                if (!Transform)
                {
                    NormalMat = device.Transform.World;
                    device.Transform.World = Matrix.Translation(Position);
                }

                device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
                for (int i = 0; i < group.Count; i++)
                {
                    device.Material = groupmtl[i];
                    device.SetTexture(0, grouptx[i]);
                    CustomVertex.PositionNormalTextured[] vtx = new CustomVertex.PositionNormalTextured[indice1[i].Count() * 3];
                    for (int ii = 0; ii < vtx.Length / 3; ii += 1)
                    {
                        vtx[(ii * 3)].Position = vert[(int)indice1[i][ii].X - 1];
                        vtx[(ii * 3)].Tu = coordinate[(int)indice1[i][ii].Y - 1].X;
                        vtx[(ii * 3)].Tv = coordinate[(int)indice1[i][ii].Y - 1].Y;
                        vtx[(ii * 3)].Normal = normal[(int)indice1[i][ii].Z - 1];
                        //vtx[(i * 3)].Color = Color.Gray.ToArgb();
                        vtx[(ii * 3) + 1].Position = vert[(int)indice2[i][ii].X - 1];
                        vtx[(ii * 3) + 1].Tu = coordinate[(int)indice2[i][ii].Y - 1].X;
                        vtx[(ii * 3) + 1].Tv = coordinate[(int)indice2[i][ii].Y - 1].Y;
                        vtx[(ii * 3) + 1].Normal = normal[(int)indice2[i][ii].Z - 1];
                        //vtx[(i * 3) + 1].Color = Color.Gray.ToArgb();
                        vtx[(ii * 3) + 2].Position = vert[(int)indice3[i][ii].X - 1];
                        vtx[(ii * 3) + 2].Tu = coordinate[(int)indice3[i][ii].Y - 1].X;
                        vtx[(ii * 3) + 2].Tv = coordinate[(int)indice3[i][ii].Y - 1].Y;
                        vtx[(ii * 3) + 2].Normal = normal[(int)indice3[i][ii].Z - 1];
                        //vtx[(i * 3) + 2].Color = Color.Gray.ToArgb();
                    }
                    device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, vtx.Length / 3, vtx);
                    Triangles += vtx.Length / 3;
                    Points += vtx.Length;
                }
                if (!Transform) { device.Transform.World = NormalMat; }
                /*device.SetStreamSource(0, vb, 0);
                device.Indices = ib;
                device.DrawIndexedPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleList, 0, 0, verts, 0, inds);*/
                device.RenderState.CullMode = Culling;
            }
        }
        public int CheckCollide(Vector3 Pos, Vector3 Size)
        {
            bool atx = false, aty = false, atz = false;
            for (int i = 0; i < Collides.Count; i++)
            {
                for (float o = Pos.X; o <= Pos.X + Size.X; o += CollidePerPoint)
                {
                    if (o >= Collides[i].Position.X && o <= Collides[i].Position.X + Collides[i].Size.X) { atx = true; }
                }
                for (float o = Pos.Y; o <= Pos.Y + Size.Y; o += CollidePerPoint)
                {
                    if (o >= Collides[i].Position.Y && o <= Collides[i].Position.Y + Collides[i].Size.Y) { aty = true; }
                }
                for (float o = Pos.Z; o <= Pos.Z + Size.Z; o += CollidePerPoint)
                {
                    if (o >= Collides[i].Position.Z && o <= Collides[i].Position.Z + Collides[i].Size.Z) { atz = true; }
                }
                if (atx && aty && atz) { return i; }
                atx = false;
                aty = false;
                atz = false;
            }
            return -1;
        }
        public string GetCollideName(int ID)
        {
            return Collides[ID].Name;
        }
        public int CheckCollideExt(Vector3 Pos, Vector3 Size, int[] IgnoredIDs)
        {
            bool atx = false, aty = false, atz = false, can = true;
            for (int i = 0; i < Collides.Count; i++)
            {
                for (int o = 0; o < IgnoredIDs.Length; o++)
                {
                    if (i == IgnoredIDs[o]) { can = false; }
                }
                if (can)
                {
                    for (float o = Pos.X; o <= Pos.X + Size.X; o += CollidePerPoint)
                    {
                        if (o >= Collides[i].Position.X && o <= Collides[i].Position.X + Collides[i].Size.X) { atx = true; }
                    }
                    for (float o = Pos.Y; o <= Pos.Y + Size.Y; o += CollidePerPoint)
                    {
                        if (o >= Collides[i].Position.Y && o <= Collides[i].Position.Y + Collides[i].Size.Y) { aty = true; }
                    }
                    for (float o = Pos.Z; o <= Pos.Z + Size.Z; o += CollidePerPoint)
                    {
                        if (o >= Collides[i].Position.Z && o <= Collides[i].Position.Z + Collides[i].Size.Z) { atz = true; }
                    }
                    if (atx && aty && atz) { return i; }
                    atx = false;
                    aty = false;
                    atz = false;
                }
                can = true;
            }
            return -1;
        }
        public int CheckCollideName(Vector3 Pos, Vector3 Size, string[] Names)
        {
            bool atx = false, aty = false, atz = false, can = false;
            for (int i = 0; i < Collides.Count; i++)
            {
                for (int o = 0; o < Names.Length; o++)
                {
                    if (CollideName[i] == Names[o]) { can = true; }
                }
                if (can)
                {
                    for (float o = Pos.X; o <= Pos.X + Size.X; o += CollidePerPoint)
                    {
                        if (o >= Collides[i].Position.X && o <= Collides[i].Position.X + Collides[i].Size.X) { atx = true; }
                    }
                    for (float o = Pos.Y; o <= Pos.Y + Size.Y; o += CollidePerPoint)
                    {
                        if (o >= Collides[i].Position.Y && o <= Collides[i].Position.Y + Collides[i].Size.Y) { aty = true; }
                    }
                    for (float o = Pos.Z; o <= Pos.Z + Size.Z; o += CollidePerPoint)
                    {
                        if (o >= Collides[i].Position.Z && o <= Collides[i].Position.Z + Collides[i].Size.Z) { atz = true; }
                    }
                    if (atx && aty && atz) { return i; }
                    atx = false;
                    aty = false;
                    atz = false;
                }
                can = false;
            }
            return -1;
        }
        public int CheckCollideIgnoreName(Vector3 Pos, Vector3 Size, string[] Names)
        {
            bool atx = false, aty = false, atz = false, can = true;
            for (int i = 0; i < Collides.Count; i++)
            {
                for (int o = 0; o < Names.Length; o++)
                {
                    if (CollideName[i] == Names[o]) { can = false; }
                }
                if (can)
                {
                    for (float o = Pos.X; o <= Pos.X + Size.X; o += CollidePerPoint)
                    {
                        if (o >= Collides[i].Position.X && o <= Collides[i].Position.X + Collides[i].Size.X) { atx = true; }
                    }
                    for (float o = Pos.Y; o <= Pos.Y + Size.Y; o += CollidePerPoint)
                    {
                        if (o >= Collides[i].Position.Y && o <= Collides[i].Position.Y + Collides[i].Size.Y) { aty = true; }
                    }
                    for (float o = Pos.Z; o <= Pos.Z + Size.Z; o += CollidePerPoint)
                    {
                        if (o >= Collides[i].Position.Z && o <= Collides[i].Position.Z + Collides[i].Size.Z) { atz = true; }
                    }
                    if (atx && aty && atz) { return i; }
                    atx = false;
                    aty = false;
                    atz = false;
                }
                can = true;
            }
            return -1;
        }
        public int CheckCollideBoth(Vector3 Pos, Vector3 Size, int[] IgnoredIDs, string[] IgnoredNames)
        {
            bool atx = false, aty = false, atz = false, can = true;
            for (int i = 0; i < Collides.Count; i++)
            {
                for (int o = 0; o < IgnoredNames.Length; o++)
                {
                    if (CollideName[i] == IgnoredNames[o]) { can = false; }
                }
                for (int o = 0; o < IgnoredIDs.Length; o++)
                {
                    if (i == IgnoredIDs[o]) { can = false; }
                }
                if (can)
                {
                    for (float o = Pos.X; o <= Pos.X + Size.X; o += CollidePerPoint)
                    {
                        if (o >= Collides[i].Position.X && o <= Collides[i].Position.X + Collides[i].Size.X) { atx = true; }
                    }
                    for (float o = Pos.Y; o <= Pos.Y + Size.Y; o += CollidePerPoint)
                    {
                        if (o >= Collides[i].Position.Y && o <= Collides[i].Position.Y + Collides[i].Size.Y) { aty = true; }
                    }
                    for (float o = Pos.Z; o <= Pos.Z + Size.Z; o += CollidePerPoint)
                    {
                        if (o >= Collides[i].Position.Z && o <= Collides[i].Position.Z + Collides[i].Size.Z) { atz = true; }
                    }
                    if (atx && aty && atz) { return i; }
                    atx = false;
                    aty = false;
                    atz = false;
                }
                can = true;
            }
            return -1;
        }
        public int CheckCollideZ(Vector3 Pos, Vector3 Size)
        {
            bool atz = false;
            for (int i = 0; i < Collides.Count; i++)
            {
                for (float o = Pos.Z; o <= Pos.Z + Size.Z; o += CollidePerPoint)
                {
                    if (o >= Collides[i].Position.Z && o <= Collides[i].Position.Z + Collides[i].Size.Z) { atz = true; }
                }
                if (atz) { return i; }
                atz = false;
            }
            return -1;
        }
        public void CollideController(float PerPoint)
        {
            CollidePerPoint = PerPoint;
        }
        public void CollideControllerDefault()
        {
            CollidePerPoint = 0.01f;
        }
        public void AddCollide(Vector3 Pos, Vector3 Size)
        {
            Collides.Add(new CollideObj(Pos, Size));
            CollideName.Add("");
            FakeCollides.Add(Collides.Count - 1);
        }
        public int AddRealCollideID(Vector3 Pos, Vector3 Size)
        {
            Collides.Add(new CollideObj(Pos, Size));
            CollideName.Add("");
            return Collides.Count - 1;
        }
        public bool CheckPosition(Vector3 Position, Vector3 From, Vector3 To)
        {
            if (Position.X >= From.X && Position.X <= To.X && Position.Y >= From.Y && Position.Y <= To.Y && Position.Z >= From.Z && Position.Z <= To.Z)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void CollideState(bool Value)
        {
            Collide = Value;
        }
        public void DrawText(int X, int Y, string Text, Color Color)
        {
            Font.DrawText(null, Text, new Point(X, Y), Color);
        }
        public void DrawingFont(System.Drawing.Font Fnt)
        {
            Font = new Microsoft.DirectX.Direct3D.Font(device, Fnt);
            OFont = Fnt;
        }
        public Vector2 TextSize(string Text)
        {
            return new Vector2(WndG.MeasureString(Text, OFont).Width, WndG.MeasureString(Text, OFont).Height);
        }
        public void DrawSprite(int X, int Y, string Name)
        {
            Sprite Spr = Sprites.Get(Name);
            Spr.Begin(SpriteFlags.AlphaBlend);
            //spr.Draw2D(Spr, Rectangle.Empty, Rectangle.Empty, new Point(X, Y), Color.White);
            Spr.Draw2D(Sprites.Texture(Name), new Point(0), 0f, new Point(X, Y), Color.White);
            Spr.End();
        }
        public void Billboard(Vector3 Position, string Name)
        {
            Sprite Spr = Sprites.Get(Name);
            Spr.Begin(SpriteFlags.Billboard|SpriteFlags.AlphaBlend);
            Spr.Draw(Sprites.Texture(Name), Vector3.Empty, Position, Color.White.ToArgb());
            Spr.End();
        }
        public int MouseGet(int Var)
        {
            if (msdevice != null)
            {
                MouseState mss = msdevice.CurrentMouseState;
                if (Var == Variable.MOUSE_X)
                {
                    int pos = Cursor.Position.X - Wnd.Location.X - 8;
                    if (pos > Wnd.Size.Width - 8) { pos = Wnd.Size.Width - 8; }
                    else if (pos < 0) { pos = 0; }
                    return pos;
                }
                else if (Var == Variable.MOUSE_Y)
                {
                    int pos = Cursor.Position.Y - Wnd.Location.Y - 30;
                    if (pos > Wnd.Size.Height - 30) { pos = Wnd.Size.Height - 30; }
                    else if (pos < 0) { pos = 0; }
                    return pos;
                }
                else if (Var == Variable.MOUSE_OLD_X)
                {
                    return (int)MouseOld.X;
                }
                else if (Var == Variable.MOUSE_OLD_Y)
                {
                    return (int)MouseOld.Y;
                }
                else if (Var == Variable.MOUSE_WND_X)
                {
                    return Cursor.Position.X;
                }
                else if (Var == Variable.MOUSE_WND_Y)
                {
                    return Cursor.Position.Y;
                }
                else if (Var == Variable.MOUSE_BUTTON)
                {
                    byte[] byt = mss.GetMouseButtons();
                    int res = 0;
                    if (byt[0] == 128) { res += Variable.MOUSE_BUTTON_LEFT; }
                    if (byt[1] == 128) { res += Variable.MOUSE_BUTTON_RIGHT; }
                    if (byt[2] == 128) { res += Variable.MOUSE_BUTTON_WHEEL; }
                    return res;
                }
                else if (Var == Variable.MOUSE_XSPD)
                {
                    return mss.X;
                }
                else if (Var == Variable.MOUSE_YSPD)
                {
                    return mss.Y;
                }
                else if (Var == Variable.MOUSE_WHEEL)
                {
                    return mss.Z;
                }
            }
            return 0;
        }
        public void MouseSet(int Var, int Value)
        {
            if (msdevice != null)
            {
                if (Var == Variable.MOUSE_WND_X)
                {
                    Cursor.Position = new Point(Value, Cursor.Position.Y);
                }
                else if (Var == Variable.MOUSE_WND_Y)
                {
                    Cursor.Position = new Point(Cursor.Position.X, Value);
                }
            }
        }
        public void LightPoint(Vector3 Position, Color Clr, float Range, float Att0 = 0.1f, float Att1 = 0f, float Att2 = 0f)
        {
            bool can = true;
            Position = new Vector3(TransformPos.X + Position.X, TransformPos.Y + Position.Y, TransformPos.Z + Position.Z);
            if (!CheckPosition(Position, new Vector3(CameraAt[CameraOn].X - FarRender, CameraAt[CameraOn].Y - FarRender, CameraAt[CameraOn].Z - FarRender), new Vector3(CameraAt[CameraOn].X + FarRender, CameraAt[CameraOn].Y + FarRender, CameraAt[CameraOn].Z + FarRender))) { can = false; }
            if (can)
            {
                device.Lights[Lights].Type = LightType.Point;
                device.Lights[Lights].Ambient = Clr;
                device.Lights[Lights].Diffuse = Clr;
                device.Lights[Lights].Specular = Color.White;
                device.Lights[Lights].Range = Range;
                device.Lights[Lights].Position = Position;
                device.Lights[Lights].Attenuation0 = Att0;
                device.Lights[Lights].Attenuation1 = Att1;
                device.Lights[Lights].Attenuation2 = Att2;
                device.Lights[Lights].Enabled = true;
                Lights++;
            }
        }
        public void LightDirection(Vector3 Direction, Color Clr)
        {
            //Position = new Vector3(TransformPos.X + Position.X, TransformPos.Y + Position.Y, TransformPos.Z + Position.Z);
            device.Lights[Lights].Type = LightType.Directional;
            //device.Lights[Lights].Range = Range;
            device.Lights[Lights].Ambient = Clr;
            device.Lights[Lights].Diffuse = Clr;
            device.Lights[Lights].Direction = Direction;
            //device.Lights[Lights].Position = Position;
            device.Lights[Lights].Enabled = true;
            Lights++;
        }
        public void LightSpot(Vector3 Position, Color Clr, Vector3 Direction)
        {
            Position = new Vector3(TransformPos.X + Position.X, TransformPos.Y + Position.Y, TransformPos.Z + Position.Z);
            device.Lights[Lights].Type = LightType.Spot;
            device.Lights[Lights].Ambient = Clr;
            device.Lights[Lights].Diffuse = Clr;
            device.Lights[Lights].Specular = Color.White;
            device.Lights[Lights].Direction = Direction;
            device.Lights[Lights].Position = Position;
            device.Lights[Lights].Attenuation0 = 0.1f;
            device.Lights[Lights].Enabled = true;
            Lights++;
        }
        public void SetTextureFolder(string Folder)
        {
            if (Folder.Substring(Folder.Length - 1, 1) != "/") { Folder = Folder + "/"; }
            TextureFolder = Folder;
        }
        public void SetModelFolder(string Folder)
        {
            if (Folder.Substring(Folder.Length - 1, 1) != "/") { Folder = Folder + "/"; }
            ModelFolder = Folder;
        }
        public void SetSoundFolder(string Folder)
        {
            if (Folder.Substring(Folder.Length - 1, 1) != "/") { Folder = Folder + "/"; }
            SoundFolder = Folder;
        }
        public float DegToRad(float Direction)
        {
            return (float)(Math.PI / 180) * Direction;
        }
        public void SetAmbient(Color Cl)
        {
            AmbientColor = Cl;
        }
        public void DrawVideoFloor(Video vid, float X, float Y, float Z, float X2, float Y2, float Z2)
        {
            vid.TextureReadyToRender += new TextureRenderEventHandler(vid_TextureReadyToRender_Floor);
            VideoRenderPos = new Vector3(X, Y, Z);
            VideoRenderTo = new Vector3(X2, Y2, Z2);
        }
        void vid_TextureReadyToRender_Floor(object sender, TextureRenderEventArgs e)
        {
            Texture videoTexture = e.Texture;
            CustomVertex.PositionTextured[] vtx = new CustomVertex.PositionTextured[4];
            //Vertexes
            vtx[0] = new CustomVertex.PositionTextured(VideoRenderPos, 0, 0);
            vtx[1] = new CustomVertex.PositionTextured(new Vector3(VideoRenderTo.X, VideoRenderPos.Y, VideoRenderTo.Z), 1, 0);
            vtx[2] = new CustomVertex.PositionTextured(new Vector3(VideoRenderTo.X, VideoRenderTo.Y, VideoRenderTo.Z), 1, 1);
            vtx[3] = new CustomVertex.PositionTextured(new Vector3(VideoRenderPos.X, VideoRenderTo.Y, VideoRenderPos.Z), 0, 1);
            //-------
            device.SetTexture(0, videoTexture);
            device.VertexFormat = CustomVertex.PositionTextured.Format;
            device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleFan, 4, vtx);
        }
        public void DrawVideoWall(Video vid, float X, float Y, float Z, float X2, float Y2, float Z2)
        {
            vid.TextureReadyToRender += new TextureRenderEventHandler(vid_TextureReadyToRender_Wall);
            VideoRenderPos = new Vector3(X, Y, Z);
            VideoRenderTo = new Vector3(X2, Y2, Z2);
        }
        void vid_TextureReadyToRender_Wall(object sender, TextureRenderEventArgs e)
        {
            /*Texture videoTexture = e.Texture;
            CustomVertex.PositionTextured[] vtx = new CustomVertex.PositionTextured[4];
            //Vertexes
            vtx[0] = new CustomVertex.PositionTextured(VideoRenderPos, 0, 0);
            vtx[1] = new CustomVertex.PositionTextured(new Vector3(VideoRenderTo.X, VideoRenderPos.Y, VideoRenderPos.Z), 1, 0);
            vtx[2] = new CustomVertex.PositionTextured(new Vector3(VideoRenderTo.X, VideoRenderTo.Y, VideoRenderTo.Z), 1, 1);
            vtx[3] = new CustomVertex.PositionTextured(new Vector3(VideoRenderPos.X, VideoRenderTo.Y, VideoRenderTo.Z), 0, 1);
            //-------
            device.SetTexture(0, videoTexture);
            device.VertexFormat = CustomVertex.PositionTextured.Format;
            device.DrawUserPrimitives(Microsoft.DirectX.Direct3D.PrimitiveType.TriangleFan, 4, vtx);*/
            MessageBox.Show("ready");
        }
        public Obj CreateObject(Vector3 Position)
        {
            Obj Object = new Obj();
            Object.Physics = Physics;
            Object.Gravity = Gravity;
            Object.Position = Position;
            Object.RotationSpeed = new Vector3(0f, 0f, 0f);
            Object.ID = Objects.Count;
            Objects.Add(Object);
            return Objects[Objects.Count - 1];
        }
        public void DestroyObject(int ID)
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].ID == ID) { if (Objects.Count > i) { Objects.RemoveAt(ID); return; } }
            }
        }
        public void DestroyAllObjects()
        {
            Objects.Clear();
        }
        public void DrawCollides()
        {
            device.RenderState.FillMode = FillMode.WireFrame;
            NormalCollide = Collide;
            Collide = false;
            for (int i = 0; i < Collides.Count; i++)
            {
                Cube(new Vector3(Collides[i].Position.X, Collides[i].Position.Y, Collides[i].Position.Z), new Vector3(Collides[i].Position.X + Collides[i].Size.X, Collides[i].Position.Y + Collides[i].Size.Y, Collides[i].Position.Z + Collides[i].Size.Z), null);
            }
            Collide = NormalCollide;
            device.RenderState.FillMode = FillMode.Solid;
        }
        public void TakeScreenshot(string Filename)
        {
            Surface surface = device.GetBackBuffer(0, 0, BackBufferType.Mono);
            if (Filename.Contains("."))
            {
                string type = Filename.Substring(Filename.LastIndexOf(".") + 1, Filename.Length - Filename.LastIndexOf(".") - 1);
                if (type == "jpg") { SurfaceLoader.Save(Filename, ImageFileFormat.Jpg, surface); }
                else if (type == "bmp") { SurfaceLoader.Save(Filename, ImageFileFormat.Bmp, surface); }
                else if (type == "png") { SurfaceLoader.Save(Filename, ImageFileFormat.Png, surface); }
                else if (type == "tga") { SurfaceLoader.Save(Filename, ImageFileFormat.Tga, surface); }
                else if (type == "dds") { SurfaceLoader.Save(Filename, ImageFileFormat.Dds, surface); }
            }
            else
            {
                SurfaceLoader.Save(Filename + ".jpg", ImageFileFormat.Jpg, surface);
            }
        }
        public int TimerCreate(int Interval)
        {
            Timers Tim = new Timers();
            Tim.Interval=Interval;
            Timer.Add(Tim);
            return Timer.Count - 1;
        }
        public bool TimerDestroy(int ID)
        {
            if (Timer.Count >= ID)
            {
                Timer.RemoveAt(ID);
                return true;
            }
            return false;
        }
        public bool TimerTick(int ID)
        {
            if (Timer.Count >= ID)
            {
                if (Timer[ID].Frame == Timer[ID].Interval) { return true; }
                return false;
            }
            return false;
        }
        public void FogEnable(Color Color, Vector2 Coords)
        {
            device.RenderState.FogEnable = true;
            device.RenderState.FogColor = Color;
            device.RenderState.FogStart = Coords.X;
            device.RenderState.FogEnd = Coords.Y;
            device.RenderState.RangeFogEnable = true;
            device.RenderState.FogVertexMode = FogMode.Linear;
        }
        public void FogDisable()
        {
            device.RenderState.FogEnable = false;
        }
        public void CameraTask(Vector3 Position, Vector3 Look, float Frames)
        {
            if (CameraTasks.Count > 0)
            {
                if (CameraTasks[CameraTasks.Count - 1].Position == Position && CameraTasks[CameraTasks.Count - 1].Look == Look) { return; }
            }
            CameraTasks.Add(new CameraTsk(Position, Look, Frames));
            CamAt = Position;
            CamLook = Look;
            CamTask = true;
        }
        public EffectObj CreateEffect(Vector3 From, Vector3 To, int Type)
        {
            EffectObj Obj = new EffectObj();
            Obj.From = From;
            Obj.To = To;
            Obj.Type = Type;
            Effect.Add(Obj);
            return Obj;
        }
        public void SceneTextureStart(Vector3 Position, Vector3 LookAt, Vector2 Size)
        {
            
            renderToSurface = new RenderToSurface(device, (int)Size.X, (int)Size.Y, Format.X8R8G8B8, true, DepthFormat.D16);
            renderTexture = new Texture(device, (int)Size.X, (int)Size.Y, 1, Usage.RenderTarget, Format.X8R8G8B8, Pool.Default);
            renderSurface = renderTexture.GetSurfaceLevel(0);
            renderToSurface.BeginScene(renderSurface);
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            device.Transform.Projection = Matrix.PerspectiveFovLH((float)Math.PI / 4, (float)((float)Wnd.Size.Width / (float)Wnd.Size.Height), NearRender, FarRender);
            device.Transform.View = Matrix.LookAtLH(Position, LookAt, new Vector3(0f, 0f, 1f));
        }
        public Texture SceneTextureEnd()
        {
            renderToSurface.EndScene(Filter.None);
            return renderTexture;
        }
    }
    public class Variable
    {
        public static int ROTATION_X = 200, ROTATION_Y = 201, ROTATION_Z = 202, ROTATION_AXIS = 203, ROTATION_AXIS_W = 204, TRANSLATION = 205;
        public static int MATERIAL_LENGTH = 100;
        public static int MOUSE_X = 0, MOUSE_Y = 1, MOUSE_OLD_X = 8, MOUSE_OLD_Y = 9, MOUSE_XSPD = 5, MOUSE_YSPD = 6, MOUSE_WHEEL = 7, MOUSE_WND_X = 2, MOUSE_WND_Y = 3, MOUSE_BUTTON = 4;
        public static int MOUSE_BUTTON_LEFT = 20, MOUSE_BUTTON_RIGHT = 21, MOUSE_BUTTON_WHEEL = 22;
        public static int MOUSE_LEFT = 8, MOUSE_MIDDLE = 9, MOUSE_RIGHT = 10;
        public static int RENDER_NEAR = 11, RENDER_FAR = 12;
        public static int NONE = 1000;
        public static int STATUS_STANDING = 300, STATUS_FLYING = 301;
        public static int VERTEXPROCESS_SOFTWARE = 400, VERTEXPROCESS_HARDWARE = 401;
        public static int REFRESH_ALL = 500, REFRESH_FILTER = 501, REFRESH_MATERIAL = 502, REFRESH_SETTINGS = 503;
        public static int EFFECT_RAIN = 600, EFFECT_SNOW = 601;
    }
    public class Textures
    {
        private static List<Texture> Texture = new List<Texture>();
        private static List<string> TextureName = new List<string>();
        public static Texture Get(string Name)
        {
            int id = -1;
            for (int i = 0; i < Texture.Count; i++)
            {
                if (TextureName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return Texture[id];
            }
            else
            {
                if (File.Exists(Main.TextureFolder + Name))
                {
                    Texture.Add(TextureLoader.FromFile(Main.device, Main.TextureFolder + Name));
                    TextureName.Add(Name);
                    return Texture[Texture.Count - 1];
                }
                else
                {
                    return null;
                }
            }
        }
        public static void Load(string Name)
        {
            int id = -1;
            for (int i = 0; i < Texture.Count; i++)
            {
                if (TextureName[i] == Name)
                {
                    id = i;
                }
            }
            if (id == -1)
            {
                if (File.Exists(Main.TextureFolder + Name))
                {
                    Texture.Add(TextureLoader.FromFile(Main.device, Main.TextureFolder + Name));
                    TextureName.Add(Name);
                }
            }
        }
        public static Texture GetUrl(string Name, string Url)
        {
            int id = -1;
            for (int i = 0; i < Texture.Count; i++)
            {
                if (TextureName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return Texture[id];
            }
            else
            {
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(Url);
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                Image img = Image.FromStream(res.GetResponseStream());
                res.Close();
                Bitmap bmp = new Bitmap(img);
                bmp.MakeTransparent(Color.Black);
                //Texture[Texs] = TextureLoader.FromStream(Main.device, res.GetResponseStream());
                Texture.Add(new Texture(Main.device, bmp, Usage.Dynamic, Pool.Default));
                TextureName.Add(Name);
                return Texture[Texture.Count - 1];
            }
        }
    }
    public class Sprites
    {
        private static List<Sprite> Spr = new List<Sprite>();
        private static List<string> SprName = new List<string>();
        private static List<Texture> SprTexture = new List<Texture>();
        public static Sprite Get(string Name)
        {
            int id = -1;
            for (int i = 0; i < Spr.Count; i++)
            {
                if (SprName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return Spr[id];
            }
            else
            {
                Spr.Add(new Sprite(Main.device));
                SprTexture.Add(Textures.Get(Name));
                SprName.Add(Name);
                return Spr[Spr.Count - 1];
            }
        }
        public static Texture Texture(string Name)
        {
            int id = -1;
            for (int i = 0; i < Spr.Count; i++)
            {
                if (SprName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return SprTexture[id];
            }
            return null;
        }
    }
    public class Sounds
    {
        private static List<SecondaryBuffer> Sound = new List<SecondaryBuffer>();
        private static List<Audio> SoundAudio = new List<Audio>();
        private static List<string> SoundName = new List<string>();
        private static List<int> SoundID = new List<int>();
        public static void Play(string Name)
        {
            int id = -1;
            string type;
            type = Name.Substring(Name.LastIndexOf(".") + 1, Name.Length - Name.LastIndexOf(".") - 1);
            for (int i = 0; i < Sound.Count; i++)
            {
                if (SoundName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                if (type == "mp3")
                {
                    SoundAudio[SoundID[id]].Play();
                }
                else
                {
                    Sound[SoundID[id]].Play(0, BufferPlayFlags.Default);
                }
            }
            else
            {
                if (File.Exists(Main.SoundFolder + Name))
                {
                    if (type == "mp3")
                    {
                        Audio snd = new Audio(Main.SoundFolder + Name);
                        SoundAudio.Add(snd);
                        SoundName.Add(Name);
                        snd.Play();
                        SoundID.Add(SoundAudio.Count - 1);
                    }
                    else
                    {
                        BufferDescription desc = new BufferDescription();
                        desc.ControlEffects = false;
                        SecondaryBuffer snd = new SecondaryBuffer(Main.SoundFolder + Name, desc, Main.sdevice);
                        Sound.Add(snd);
                        SoundName.Add(Name);
                        snd.Play(0, BufferPlayFlags.Default);
                        SoundID.Add(Sound.Count - 1);
                    }
                }
            }
        }
        public static void Load(string Name)
        {
            int id = -1;
            string type = Name.Substring(Name.LastIndexOf(".") + 1, Name.Length - Name.LastIndexOf(".") - 1);
            for (int i = 0; i < Sound.Count; i++)
            {
                if (SoundName[i] == Name)
                {
                    id = i;
                }
            }
            if (id == -1)
            {
                if (File.Exists(Main.SoundFolder + Name))
                {
                    if (type == "mp3")
                    {
                        Audio snd = new Audio(Main.SoundFolder + Name);
                        SoundAudio.Add(snd);
                        SoundName.Add(Name);
                        SoundID.Add(SoundAudio.Count - 1);
                    }
                    else
                    {
                        BufferDescription desc = new BufferDescription();
                        desc.ControlEffects = false;
                        SecondaryBuffer snd = new SecondaryBuffer(Main.SoundFolder + Name, desc, Main.sdevice);
                        Sound.Add(snd);
                        SoundName.Add(Name);
                        SoundID.Add(Sound.Count - 1);
                    }
                }
            }
        }
        public static void Stop(string Name)
        {
            int id = -1;
            string type = Name.Substring(Name.LastIndexOf(".") + 1, Name.Length - Name.LastIndexOf(".") - 1);
            for (int i = 0; i < Sound.Count; i++)
            {
                if (SoundName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                if (type == "mp3")
                {
                    SoundAudio[SoundID[id]].Stop();
                }
                else
                {
                    Sound[SoundID[id]].Stop();
                }
            }
        }
        public static void Delete(string Name)
        {
            int id = -1;
            for (int i = 0; i < Sound.Count; i++)
            {
                if (SoundName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                Sound[id].Dispose();
                Sound.RemoveAt(id);
                SoundName.RemoveAt(id);
            }
        }
    }
    public class Meshes
    {
        private static List<Mesh> Msh = new List<Mesh>();
        private static List<string[]> MshStr = new List<string[]>();
        private static List<string> MshType = new List<string>();
        private static List<Material[]> MshMat = new List<Material[]>();
        private static List<Texture[]> MshTex = new List<Texture[]>();
        private static List<string[]> MshMatName = new List<string[]>();
        private static List<int> MshMats = new List<int>();
        private static List<string> MeshName = new List<string>();
        private static List<Vector3> ObjVertices = new List<Vector3>(), ObjNormals = new List<Vector3>();
        private static List<Vector3[]> ObjFaces = new List<Vector3[]>();
        private static List<Vector2> ObjUVs = new List<Vector2>();
        /*private static void ExecuteDmf(string Script)
        {
            if (Script.Contains(";"))
            {
                string scr, func, args;
                string[] arg;
                scr = Script.Substring(0, Script.IndexOf(";"));
                func = scr.Substring(0, scr.IndexOf(":"));
                args = scr.Substring(scr.IndexOf(":") + 1, scr.Length - scr.IndexOf(":") - 1);
                arg = args.Split(',');

                if (func == "v")
                {
                    float X, Y, Z;
                    float.TryParse(arg[0], out X);
                    float.TryParse(arg[1], out Y);
                    float.TryParse(arg[2], out Z);

                    DmfVertices[DmfVerts] = new Vector3(X, Y, Z);
                    DmfVerts++;
                }

                Script = Script.Remove(0, scr.Length + 1);
                if (Script != "") { ExecuteDmf(Script); }
            }
        }*/
        public static Mesh Get(string Name)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return Msh[id];
            }
            else
            {
                FileInfo finfo = new FileInfo(Main.ModelFolder + Name);
                string sub = finfo.Name.Substring(finfo.Name.LastIndexOf("."), finfo.Name.Length - finfo.Name.LastIndexOf("."));
                if (sub == ".x")
                {
                    ExtendedMaterial[] materials;
                    List<Texture> texs = new List<Texture>();
                    List<Material> mts = new List<Material>();
                    Msh.Add(Mesh.FromFile(Main.ModelFolder + Name, MeshFlags.Dynamic, Main.device, out materials));
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i].TextureFilename != "" && materials[i].TextureFilename != String.Empty && materials[i].TextureFilename != null)
                        {
                            texs.Add(Textures.Get(materials[i].TextureFilename));
                            Material matr = materials[i].Material3D;
                            matr.Ambient=matr.Diffuse;
                            mts.Add(matr);
                        }
                    }
                    MshTex.Add(texs.ToArray());
                    MshMat.Add(mts.ToArray());
                    MshMats.Add(materials.Length);
                    MshMatName.Add(null);
                    MeshName.Add(Name);
                    MshType.Add(".x");

                    return Msh[Msh.Count - 1];
                }
                /*else if (sub == ".ms3d")
                {

                }*/
                else if (sub == ".dmf")
                {
                    if (File.Exists(Main.ModelFolder + Name))
                    {
                        string[] Scr = File.ReadAllLines(Main.ModelFolder + Name);
                        Msh.Add(Mesh.Box(Main.device, 0f, 0f, 0f));
                        MshStr.Add(Scr);
                        MeshName.Add(Name);
                        MshTex.Add(null);
                        MshMat.Add(null);
                        MshMatName.Add(null);
                        MshType.Add(".dmf");

                        return Msh[Msh.Count - 1];
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (sub == ".obj")
                {
                    if (File.Exists(Main.ModelFolder + Name))
                    {
                        string[] scr = File.ReadAllLines(Main.ModelFolder + Name);
                        MshStr.Add(scr);
                        Msh.Add(Mesh.Box(Main.device, 0f, 0f, 0f));
                        MshTex.Add(null);
                        MshMat.Add(null);
                        MshMatName.Add(null);
                        /*string sc, func, args;
                        string[] arg;
                        Vector3[] vert = new Vector3[10000];
                        Vector3[,] indice = new Vector3[10000, 3];
                        int verts = 0, inds = 0;
                        for (int i = 0; i < scr.Length; i++)
                        {
                            if (scr[i] != "" || scr[i] != String.Empty)
                            {
                                sc = scr[i];
                                func = sc.Substring(0, sc.IndexOf(" "));
                                args = sc.Substring(sc.IndexOf(" ") + 1, sc.Length - sc.IndexOf(" ") - 1);
                                arg = args.Split(' ');
                                if (func == "v")
                                {
                                    float X, Y, Z;
                                    float.TryParse(arg[0], out X);
                                    float.TryParse(arg[1], out Y);
                                    float.TryParse(arg[2], out Z);
                                    vert[verts] = new Vector3(X, Y, Z);
                                    verts++;
                                }
                                else if (func == "f")
                                {
                                    string[] arg1 = arg[0].Split('/');
                                    string[] arg2 = arg[1].Split('/');
                                    string[] arg3 = arg[2].Split('/');
                                    int X, Y, Z, X2, Y2, Z2, X3, Y3, Z3;
                                    int.TryParse(arg1[0], out X);
                                    int.TryParse(arg1[1], out Y);
                                    int.TryParse(arg1[2], out Z);
                                    int.TryParse(arg2[0], out X2);
                                    int.TryParse(arg2[1], out Y2);
                                    int.TryParse(arg2[2], out Z2);
                                    int.TryParse(arg3[0], out X3);
                                    int.TryParse(arg3[1], out Y3);
                                    int.TryParse(arg3[2], out Z3);
                                    indice[inds, 0] = new Vector3(X, Y, Z);
                                    indice[inds, 1] = new Vector3(X2, Y2, Z2);
                                    indice[inds, 2] = new Vector3(X3, Y3, Z3);
                                    inds++;
                                }
                            }
                        }
                        Msh[Meshs] = new Mesh(inds * 3, verts, MeshFlags.Managed, CustomVertex.PositionTextured.Format, Main.device);
                        //VertexBuffer vb = Msh[Meshs].VertexBuffer;
                        //IndexBuffer ib = Msh[Meshs].IndexBuffer;
                        using (VertexBuffer vb = Msh[Meshs].VertexBuffer)
                        {
                            GraphicsStream data = vb.Lock(0, 0, LockFlags.None);
                            //CustomVertex.PositionTextured[] vtx = new CustomVertex.PositionTextured[verts];
                            for (int i = 0; i < verts; i++)
                            {
                                //vtx[i].Position = vert[i];
                                data.Write(new CustomVertex.PositionTextured(vert[i], 0, 0));
                            }
                            vb.Unlock();
                        }
                        //vb.SetData(vtx, 0, LockFlags.None);
                        int[] idx = new int[inds * 3];
                        for (int i = 0; i < inds; i++)
                        {
                            idx[(i * 3)] = (int)indice[i, 0].X - 1;
                            idx[(i * 3) + 1] = (int)indice[i, 1].X - 1;
                            idx[(i * 3) + 2] = (int)indice[i, 2].X - 1;
                        }
                        using (IndexBuffer ib = Msh[Meshs].IndexBuffer)
                        {
                            ib.SetData(idx, 0, LockFlags.None);
                        }
                        int[] attrBuffer = Msh[Meshs].LockAttributeBufferArray(LockFlags.None);
                        for (int i = 0; i < inds; i++)
                        {
                            attrBuffer[i] = 0;
                        }
                        Msh[Meshs].UnlockAttributeBuffer(attrBuffer);
                        AttributeRange sub0 = new AttributeRange();
                        sub0.AttributeId = 0;
                        sub0.FaceStart = 0;
                        sub0.FaceCount = inds;
                        sub0.VertexCount = verts;
                        sub0.VertexStart = 0;

                        Msh[Meshs].SetAttributeTable(new AttributeRange[] { sub0 });

                        //Msh[Meshs].ComputeNormals();
                        //MshObj[Meshs] = Scr;*/
                        MeshName.Add(Name);
                        MshType.Add(".obj");

                        return Msh[Msh.Count - 1];
                    }
                    else
                    {
                        return null;
                    }
                }
                return null;
            }
        }
        public static string GetInner(string Name)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return "";
            }
            else
            {
                return "";
            }
        }
        public static string[] GetLineInner(string Name)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return MshStr[id];
            }
            else
            {
                return null;
            }
        }
        public static string GetType(string Name)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return MshType[id];
            }
            else
            {
                return "";
            }
        }
        public static int GetLength(int Var, string Name)
        {
            if (Var == Variable.MATERIAL_LENGTH)
            {
                int id = -1;
                for (int i = 0; i < Msh.Count; i++)
                {
                    if (MeshName[i] == Name)
                    {
                        id = i;
                    }
                }
                if (id != -1)
                {
                    return MshMats[id];
                }
                else
                {
                    return 0;
                }
            }
            return 0;
        }
        public static bool HaveMaterial(string Name)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                if (MshMat[id] != null) { return true; }
                else { return false; }
            }
            return false;
        }
        public static Material GetMaterial(string Name, int iid)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return MshMat[id][iid];
            }
            else
            {
                return MshMat[0][0];
            }
        }
        public static Material[] GetMaterials(string Name)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return MshMat[id];
            }
            return null;
        }
        public static string GetMaterialName(string Name, int iid)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return MshMatName[id][iid];
            }
            return "";
        }
        public static string[] GetMaterialNames(string Name)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return MshMatName[id];
            }
            return null;
        }
        public static Texture GetTexture(string Name, int iid)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return MshTex[id][iid];
            }
            else
            {
                return MshTex[0][0];
            }
        }
        public static Texture[] GetTextures(string Name)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                return MshTex[id];
            }
            return null;
        }
        public static void SetMaterial(string Name, Material[] Material)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                MshMat[id] = Material;
            }
        }
        public static void SetMaterialName(string Name, string[] Names)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                MshMatName[id] = Names;
            }
        }
        public static void SetTexture(string Name, Texture[] Texture)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                MshTex[id] = Texture;
            }
        }
        public static void Destroy(string Name)
        {
            int id = -1;
            for (int i = 0; i < Msh.Count; i++)
            {
                if (MeshName[i] == Name)
                {
                    id = i;
                }
            }
            if (id != -1)
            {
                for (int i = id; i < Msh.Count - 1; i++)
                {
                    Msh[i] = Msh[i + 1];
                    MeshName[i] = MeshName[i + 1];
                    MshType[i] = MshType[i + 1];
                    if (MshType[i] == ".x")
                    {
                        MshMats[i] = MshMats[i + 1];
                        for (int ii = 0; ii < 10; ii++)
                        {
                            MshMat[i][ii] = MshMat[i + 1][ii];
                            MshTex[i][ii] = MshTex[i + 1][ii];
                        }
                    }
                    else if (MshType[i] == ".dmf"||MshType[i]==".obj")
                    {
                        MshStr.RemoveAt(i);
                    }
                }
            }
        }
    }
    public class Obj
    {
        public bool Physics, Collide = true;
        public Vector3 Pivot = new Vector3(0f, 0f, 0f);
        public Vector3 LastPosition, LastPositionSpeed, LastRotation, LastRotationSpeed;
        public Vector3 Position, PositionSpeed, Rotation, RotationSpeed, Size;
        public float Gravity, U, V;
        public int Status, CollideID = -1, ID = 0;
        public string Name = "";
        public List<string> IgnoreNames = new List<string>();
        public List<string> VarString = new List<string>();
        public List<int> VarInt = new List<int>();
        public List<float> VarFloat = new List<float>();
        public List<bool> VarBool = new List<bool>();
        public Texture Texture;
        public Material Material;
    }
    public class Timers
    {
        public int Interval, Frame = 0;
    }
    public class CollideObj
    {
        public Vector3 Position, Size;
        public string Name;
        public CollideObj(Vector3 Pos, Vector3 Siz)
        {
            Position = Pos;
            //Size = new Vector3(Math.Abs(Siz.X), Math.Abs(Siz.Y), Math.Abs(Siz.Z));
            Size = Siz;
        }
    }
    public class TerrainObj
    {
        public Vector3 Position;
        public Vector2 Size;
        public List<Vector3> HeightData = new List<Vector3>();
        public Texture Texture = null;
        public float Tu = 1, Tv = 1, Space = 1f;
        public TerrainObj(Vector3 Pos, Vector2 Siz)
        {
            Position = Pos;
            Size = Siz;
        }
        public void Set(Vector2 Pos, float Z)
        {
            int id = -1;
            for (int i = 0; i < HeightData.Count; i++)
            {
                if (HeightData[i].X == Pos.X && HeightData[i].Y == Pos.Y) { id = i; }
            }
            if (id != -1) { HeightData[id] = new Vector3(Pos.X, Pos.Y, Z); }
            else { HeightData.Add(new Vector3(Pos.X, Pos.Y, Z)); }
        }
        public float Get(Vector2 Pos)
        {
            float[,] Points = new float[(int)Size.X, (int)Size.Y];
            for (int i = 0; i < (int)Size.X; i++)
            {
                for (int ii = 0; ii < (int)Size.Y; ii++)
                {
                    Points[i, ii] = 0f;
                }
            }
            for (int i = 0; i < HeightData.Count; i++)
            {
                Points[(int)(HeightData[i].X*Space), (int)(HeightData[i].Y*Space)] = HeightData[i].Z;
            }
            float[,] Heights = new float[(int)Size.X + 1, (int)Size.Y + 1];
            for (int i = 0; i < (int)(Size.X - Space); i += (int)Space)
            {
                for (int ii = 0; ii < (int)(Size.Y - Space); ii += (int)Space)
                {
                    float PercX = (Points[i + (int)Space, ii] - Points[i, ii]) / Space, PercY = (Points[i, ii + (int)Space] - Points[i, ii]) / Space;
                    for (int iii = 0; iii < (int)Space; iii++)
                    {
                        for (int iiii = 0; iiii < (int)Space; iiii++)
                        {
                            Points[iii, iiii] = (iii * PercX) + (iiii * PercY);
                        }
                    }
                }
            }
            return Points[(int)Pos.X, (int)Pos.Y];
        }
    }
    public class CameraTsk
    {
        public Vector3 Position, Look, Speed1, Speed2;
        public float Frames, MaxFrames;
        public CameraTsk(Vector3 Pos, Vector3 Lk, float Frms)
        {
            Position = Pos;
            Look = Lk;
            MaxFrames = Frms;
            float dx, dy, dz, dx2, dy2, dz2;
            dx = Pos.X - Main.CamAt.X;
            dy = Pos.Y - Main.CamAt.Y;
            dz = Pos.Z - Main.CamAt.Z;
            dx2 = Lk.X - Main.CamLook.X;
            dy2 = Lk.Y - Main.CamLook.Y;
            dz2 = Lk.Z - Main.CamLook.Z;
            Speed1 = new Vector3(dx / Frms, dy / Frms, dz / Frms);
            Speed2 = new Vector3(dx2 / Frms, dy2 / Frms, dz2 / Frms);
        }
    }
    public class EffectObj
    {
        public Vector3 From, To;
        public int Type, Strength = 1; //Strength per second
        public float Speed = 1f;
        public List<Vector3> Instance = new List<Vector3>();
        /*
         * 0 = Rain
         * 1 = Snow
        */
    }
}
