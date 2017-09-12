// http://www.opengl-tutorial.org/intermediate-tutorials/billboards-particles/particles-instancing/

using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mike.Graphics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Mike.Particles
{
    public class ParticleRender
    {
        private Camera _cam;
        private Random _rand;

        // Maximum number of particles able to be drawn to the frame.
        private const int MaxParticles = 100000;

        private const int ParticlesPerSec = 10000;

        private readonly Vector3 Gravity = new Vector3(0, -1.0f, 0.01f);


        private const float speed = 5.0f;
        private const double Angle = Math.PI / 2;
        private const float _coneSize = (float) (Math.PI / 4);


        // CPU representation of a particle
        private struct Particle
        {
            public Vector3 pos, vel;
            public Color4 color;
            public float size, weight;
            public float life; // Remaining life of the particle. if < 0 : dead and unused.

            public float cameradistance;
        }

        int LastUsedParticle = 0;

        public ShaderProgram _shader;
        private int _VAO;

        private readonly Particle[] ParticlesContainer = new Particle[MaxParticles];

        private int ParticlesCount;

        // The VBO containing the 4 vertices of the particles.
        // Thanks to instancing, they will be shared by all particles.
        float[] g_vertex_buffer_data = {
            -0.5f, -0.5f, 0.0f,
            0.5f, -0.5f, 0.0f,
            -0.5f, 0.5f, 0.0f,
            0.5f, 0.5f, 0.0f,
        };

        private int billboard_vertex_buffer;
        private int particles_position_buffer;
        private int particles_color_buffer;

        private readonly float[] g_particule_position_size_data = new float[4 * MaxParticles];
        private readonly float[] g_particule_color_data = new float[4 * MaxParticles];

        public ParticleRender(Camera camera)
        {
            _cam = camera;
            _rand = new Random(DateTime.Now.Millisecond);
        }

        public void Initialize()
        {

            // make a new shader program
            var shader = new ShaderProgram();

            // add the Vertex and Fragment shaders to our program
            shader.Add(new Mike.Graphics.Shader(ShaderType.VertexShader, new FileInfo(@"Graphics/Shaders/vert_particle.gls")));
            shader.Add(new Mike.Graphics.Shader(ShaderType.FragmentShader, new FileInfo(@"Graphics/Shaders/frag_particle.gls")));

            // compile the program
            shader.Compile();

            // use this shader program for drawing VBO's from now on
            // you can call this in Render event to swap between multiple shader programs,
            // but for now we just have one, so no point re-binding it every frame.
            shader.Use();
            _shader = shader;

            //TODO: Make/bind VAO
            _VAO = GL.GenVertexArray();
            GL.BindVertexArray(_VAO);

            billboard_vertex_buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, billboard_vertex_buffer);
            GL.BufferData(BufferTarget.ArrayBuffer, g_vertex_buffer_data.Length * sizeof(float), g_vertex_buffer_data, BufferUsageHint.StaticDraw);

            // The VBO containing the positions and sizes of the particles
            particles_position_buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, particles_position_buffer);
            // Initialize with empty (NULL) buffer : it will be updated later, each frame.
            GL.BufferData(BufferTarget.ArrayBuffer, MaxParticles * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.StreamDraw);

            // The VBO containing the colors of the particles
            particles_color_buffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, particles_color_buffer);
            // Initialize with empty (NULL) buffer : it will be updated later, each frame.
            GL.BufferData(BufferTarget.ArrayBuffer, MaxParticles * 4 * sizeof(byte), IntPtr.Zero, BufferUsageHint.StreamDraw);
        }

        public void Update(float deltaTime)
        {
            // Generate 10 new particule each millisecond,
            // but limit this to 16 ms (60 fps), or if you have 1 long frame (1sec),
            // newparticles will be huge and the next frame even longer.
            int newparticles = (int)(deltaTime * ParticlesPerSec);
            if (newparticles > (int)(0.016f * ParticlesPerSec))
                newparticles = (int)(0.016f * ParticlesPerSec);

            while (newparticles > 0)
            {
                var index = FindUnusedParticle();
                ParticlesContainer[index] = new Particle
                {
                    life = 8.0f,
                    pos = Vector3.Zero,
                    vel = randCone() * speed,
                    color = randColor(),
                    cameradistance = -1.0f,
                    size = 0.15f,
                    weight = 1.0f,
                };

                newparticles--;
            }

            Simulate(deltaTime);

            UpdateBuffers();
        }

        public void Draw()
        {
            PreDraw();

            // These functions are specific to glDrawArrays*Instanced*.
            // The first parameter is the attribute buffer we're talking about.
            // The second parameter is the "rate at which generic vertex attributes advance when rendering multiple instances"
            // http://www.opengl.org/sdk/docs/man/xhtml/glVertexAttribDivisor.xml
            GL.VertexAttribDivisor(0, 0); // particles vertices : always reuse the same 4 vertices -> 0
            GL.VertexAttribDivisor(1, 1); // positions : one per quad (its center) -> 1
            GL.VertexAttribDivisor(2, 1); // color : one per quad -> 1

            // Draw the particles !
            // This draws many times a small triangle_strip (which looks like a quad).
            // This is equivalent to :
            // for(i in ParticlesCount) : glDrawArrays(GL_TRIANGLE_STRIP, 0, 4),
            // but faster.
            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, ParticlesCount);

        }

        public void Shutdown()
        {
            
        }

        // convert range [0 - 1] to [-cone - +cone]
        private Vector3 randCone()
        {
            var vals = new float[3];
            for (var i = 0; i < 3; i++)
            {
                var num = _rand.NextDouble();
                vals[i] = (float) ((num * _coneSize - _coneSize / 2) + Angle);
            }
            return new Vector3((float) Math.Cos(vals[0]), (float) Math.Sin(vals[1]), vals[2] * 1.0f);
        }

        private Color4 randColor()
        {
            var vals = new float[3];
            for (var i = 0; i < 3; i++)
            {
                vals[i] = (float)_rand.NextDouble();
            }
            return new Color4(vals[0], vals[1], vals[2], 1.0f);
        }

        private void PreDraw()
        {
            _shader.Use();
            GL.BindVertexArray(_VAO);

            // 1rst attribute buffer : vertices
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, billboard_vertex_buffer);
            GL.VertexAttribPointer(
                0, // attribute. No particular reason for 0, but must match the layout in the shader.
                3, // size
                VertexAttribPointerType.Float, // type
                false, // normalized?
                0, // stride
                0 // array buffer offset
            );

            // 2nd attribute buffer : positions of particles' centers
            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, particles_position_buffer);
            GL.VertexAttribPointer(
                1, // attribute. No particular reason for 1, but must match the layout in the shader.
                4, // size : x + y + z + size => 4
                VertexAttribPointerType.Float, // type
                false, // normalized?
                0, // stride
                0 // array buffer offset
            );

            // 3rd attribute buffer : particles' colors
            GL.EnableVertexAttribArray(2);
            GL.BindBuffer(BufferTarget.ArrayBuffer, particles_color_buffer);
            GL.VertexAttribPointer(
                2, // attribute. No particular reason for 1, but must match the layout in the shader.
                4, // size : r + g + b + a => 4
                VertexAttribPointerType.Float, // type
                false, // normalized? *** YES, this means that the unsigned char[4] will be accessible with a vec4 (floats) in the shader ***
                0, // stride
                0 // array buffer offset
            );
        }

        private void UpdateBuffers()
        {
            // Update the buffers that OpenGL uses for rendering.
            // There are much more sophisticated means to stream data from the CPU to the GPU,
            // but this is outside the scope of this tutorial.
            // http://www.opengl.org/wiki/Buffer_Object_Streaming

            GL.BindBuffer(BufferTarget.ArrayBuffer, particles_position_buffer);
            GL.BufferData(BufferTarget.ArrayBuffer, MaxParticles * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.StreamDraw); // Buffer orphaning, a common way to improve streaming perf. See above link for details.
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, ParticlesCount * sizeof(float) * 4, g_particule_position_size_data);

            GL.BindBuffer(BufferTarget.ArrayBuffer, particles_color_buffer);
            GL.BufferData(BufferTarget.ArrayBuffer, MaxParticles * 4 * sizeof(float), IntPtr.Zero, BufferUsageHint.StreamDraw); // Buffer orphaning, a common way to improve streaming perf. See above link for details.
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, ParticlesCount * sizeof(float) * 4, g_particule_color_data);
        }

        // Finds a Particle in ParticlesContainer which isn't used yet.
        // (i.e. life < 0);
        private int FindUnusedParticle()
        {

            for (int i = LastUsedParticle; i < MaxParticles; i++)
            {
                if (ParticlesContainer[i].life <= 0)
                {
                    LastUsedParticle = i;
                    return i;
                }
            }

            for (int i = 0; i < LastUsedParticle; i++)
            {
                if (ParticlesContainer[i].life <= 0)
                {
                    LastUsedParticle = i;
                    return i;
                }
            }

            return 0; // All particles are taken, override the first one
        }

        private void Simulate(float delta)
        {
            // Simulate all particles
            ParticlesCount = 0;
            var cameraPosition = _cam.CamPosition;
            for (int i = 0; i < MaxParticles; i++)
            {
                Particle p = ParticlesContainer[i]; // shortcut

                if (p.life > 0.0f)
                {
                    // Decrease life
                    p.life -= delta;
                    if (p.life > 0.0f)
                    {
                        // Simulate simple physics : gravity only, no collisions
                        p.vel += Gravity * (float)delta * p.weight;
                        p.pos += p.vel * (float)delta;
                        p.cameradistance = (p.pos - cameraPosition).LengthSquared;

                        //TODO: Sort particles

                        // Fill the GPU buffer
                        g_particule_position_size_data[4 * ParticlesCount + 0] = p.pos.X;
                        g_particule_position_size_data[4 * ParticlesCount + 1] = p.pos.Y;
                        g_particule_position_size_data[4 * ParticlesCount + 2] = p.pos.Z;

                        g_particule_position_size_data[4 * ParticlesCount + 3] = p.size;

                        g_particule_color_data[4 * ParticlesCount + 0] = p.color.R;
                        g_particule_color_data[4 * ParticlesCount + 1] = p.color.G;
                        g_particule_color_data[4 * ParticlesCount + 2] = p.color.B;
                        g_particule_color_data[4 * ParticlesCount + 3] = p.color.A;
                    }
                    else
                    {
                        // Particles that just died will be put at the end of the buffer in SortParticles();
                        p.cameradistance = -1.0f;
                    }

                    ParticlesContainer[i] = p;
                    ParticlesCount++;
                }
            }
        }
    }
}
