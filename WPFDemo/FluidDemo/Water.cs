﻿namespace WPFDemo.FluidDemo
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;
    using Physics2D;
    using Physics2D.Object;
    using WPFDemo.Graphic;

    internal class Water : IDrawable
    {
        public readonly List<Particle> ObjList = new List<Particle>();

        private const int Threshold = 900;
        private const int GridR = 60;

        private const int R = 150;

        private readonly int[] metaTable;
        private readonly int[,] cacheTable;
        private readonly object[,] cacheLocks;
        private int[,] cache;

        public Water(int maxWidth, int maxHeight)
        {
            // 计算势能函数缓存
            this.metaTable = new int[GridR];
            this.metaTable[0] = Threshold;
            for (int i = 1; i < GridR; i++)
            {
                this.metaTable[i] = R * R / (i * i);
            }

            // 计算势能缓存
            this.cacheTable = new int[2 * GridR, 2 * GridR];
            for (int i = 0; i < 2 * GridR; i++)
            {
                for (int j = 0; j < 2 * GridR; j++)
                {
                    int d = (int)Math.Sqrt((i - GridR) * (i - GridR) + (j - GridR) * (j - GridR) + 0.5);
                    this.cacheTable[i, j] = d < GridR ? this.metaTable[d] : 0;
                }
            }

            // 初始化锁
            int w = maxWidth;
            int h = maxHeight;
            this.cacheLocks = new object[w, h];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    this.cacheLocks[i, j] = new object();
                }
            }
        }

        public unsafe void Draw(WriteableBitmap bitmap)
        {
            // 绘制Metaball
            using (var wc = bitmap.GetBitmapContext())
            {
                int w = wc.Width;
                int h = wc.Height;
                var pixels = wc.Pixels;
                this.cache = this.cache ?? new int[w, h];
                Array.Clear(this.cache, 0, this.cache.Length);

                // 叠加每个球的势能
                Parallel.ForEach(this.ObjList, obj =>
                {
                    int x = obj.Position.X.ToDisplayUnits();
                    int y = obj.Position.Y.ToDisplayUnits();

                    for (int i = x - GridR, I = 0; i < x + GridR; i++, I++)
                    {
                        for (int j = y - GridR, J = 0; j < y + GridR; j++, J++)
                        {
                            if (i < 0 || i >= w || j < 0 || j >= h) continue;
                            else
                            {
                                lock (this.cacheLocks[i, j])
                                {
                                    this.cache[i, j] += this.cacheTable[I, J];
                                }
                            }
                        }
                    }
                });

                // 渲染画布
                Parallel.For(0, h, y =>
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (this.cache[x, y] >= Threshold)
                        {
                            pixels[y * w + x] = (255 << 24) | (16 << 68) | (146 << 8) | 216;
                        }
                    }
                });
            }
        }
    }
}
