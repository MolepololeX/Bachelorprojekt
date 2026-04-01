using System;
using Godot;

namespace BA
{
    public partial class DrawHelper : Node
    {
        public static void DrawGridDigitsLabel(
            Image graph,
            Texture2D digitTexture,
            Texture2D labelTexture,
            int graphRes,
            int linesX,
            int linesY,
            float Xbot,
            float Xtop,
            float Ybot,
            float Ytop,
            int thickness,
            int centerThickness,
            Color color,
            Color centerColor,
            int numberScale = 2,
            int decimalsX = 2,
            int decimalsY = 2,
            string labelX = "x Axis",
            string labelY = "y Axis"
        )
        {
            DrawGrid(graph, graphRes, linesY, linesX, thickness, color, centerThickness, centerColor);
            DrawDigits(graph, graphRes, linesX, linesY, thickness, centerThickness, digitTexture, numberScale, Ytop, Ybot, Xbot, Xtop, decimalsX, decimalsY);
            DrawLabel(graph, graphRes, labelTexture, numberScale, labelX, labelY);
        }

        private static void DrawLabel(
                   Image graph,
                   int graphRes,
                   Texture2D characterImage,
                   int scale,
                   string labelX,
                   string labelY
                   )
        {
            var characters = characterImage.GetImage();
            characters.Decompress();
            int W = characters.GetHeight();
            int H = characters.GetHeight();

            labelX = labelX.ToLower();
            labelY = labelY.ToLower();

            for (int l = 0; l < labelX.Length; l++)
            {
                int x = graphRes / 100 + H * scale;
                int y = graphRes - 1 - scale * H - (graphRes / 100);
                int digitIndex = labelX[l] - 'a';
                if (digitIndex < 0 || digitIndex > 208 / 8) continue;
                DrawCharacter(graph, characters, W, H, digitIndex, x, y, l);
            }

            graph.Rotate90(ClockDirection.Clockwise);

            for (int l = 0; l < labelY.Length; l++)
            {
                int x = graphRes / 100 + H * scale;
                int y = graphRes - 1 - scale * H - (graphRes / 100);
                int digitIndex = labelY[l] - 'a';
                if (digitIndex < 0 || digitIndex > 208 / 8) continue;
                DrawCharacter(graph, characters, W, H, digitIndex, x, y, l);
            }

            graph.Rotate90(ClockDirection.Counterclockwise);

            void DrawCharacter(Image graph, Image digits, int W, int H, int digit, int x, int y, int offsetInDigits)
            {
                x += offsetInDigits * W * scale;
                for (int i = 0; i < W * scale; i++)
                {
                    for (int j = 0; j < H * scale; j++)
                    {
                        if (x + i >= graphRes || x + i < 0) continue;
                        if (y + j >= graphRes || y + j < 0) continue;
                        var c = digits.GetPixel(i / scale + (W * digit), j / scale);
                        if (c.A == 0.0) continue;
                        graph.SetPixel(x + i, y + j, c);
                    }
                }
            }
        }

        private static void DrawDigits(
            Image graph,
            int _graphRes,
            int linesX,
            int linesY,
            int thickness,
            int centerThickness,
            Texture2D digitImage,
            int scale,
            float Ytop,
            float Ybot,
            float Xtop,
            float Xbot,
            int decimalsX,
            int decimalsY
            )
        {
            var digits = digitImage.GetImage();
            digits.Decompress();
            int W = digits.GetHeight();
            int H = digits.GetHeight();

            for (int l = 0; l < linesX; l++)
            {
                int x = (int)(l / (float)linesX * _graphRes);
                int y = (_graphRes - 1) / 2;
                y += centerThickness * 2 * scale;
                x += thickness * 2 * scale;
                DrawScaleNumber(graph, Xtop, Xbot, digits, W, H, l, x, y, linesX, decimalsX);
            }

            for (int l = 0; l < linesY; l++)
            {
                int x = 0;
                int y = (int)(l / (float)linesY * _graphRes);
                x += thickness * 2 * scale;
                y -= centerThickness * 2 * scale + H * scale;
                DrawScaleNumber(graph, Ytop, Ybot, digits, W, H, l, x, y, linesY, decimalsY);
            }

            void DrawDigit(Image graph, Image digits, int W, int H, int digit, int x, int y, int offsetInDigits)
            {
                x += offsetInDigits * W * scale;
                for (int i = 0; i < W * scale; i++)
                {
                    for (int j = 0; j < H * scale; j++)
                    {
                        if (x + i >= _graphRes || x + i < 0) continue;
                        if (y + j >= _graphRes || y + j < 0) continue;
                        var c = digits.GetPixel(i / scale + (W * digit), j / scale);
                        if (c.A == 0.0) continue;
                        graph.SetPixel(x + i, y + j, c);
                    }
                }
            }

            void DrawScaleNumber(Image graph, float Ytop, float Ybot, Image digits, int W, int H, int l, int x, int y, int gridLines, int decimals)
            {
                bool rangeNegative = false;
                int range = (int)Math.Round(Mathf.Lerp(Ytop, Ybot, l / (float)gridLines) * Math.Pow(10.0, decimals));
                if (range < 0) rangeNegative = true;
                range = Math.Abs(range);
                var rangeString = range.ToString();
                if (range == 0.0)
                {
                    rangeString = "";
                    for (int i = 0; i < decimals + 1; i++)
                    {
                        rangeString += "0";
                    }
                }
                else if (Math.Abs(range / Math.Pow(10.0, decimals)) < 1.0)
                {
                    rangeString = rangeString.Insert(0, "0");
                }
                char[] rangeDigits = rangeString.ToCharArray();
                int decimalIndex = rangeDigits.Length - decimals;



                int o = 0;
                if (rangeNegative)
                {
                    DrawDigit(graph, digits, W, H, 12, x, y, 0 + o);
                    o += 1;
                }
                for (int i = 0; i < rangeDigits.Length; i++)
                {
                    if (i == decimalIndex)
                    {
                        DrawDigit(graph, digits, W, H, 10, x, y, i + o);
                        o += 1;
                    }
                    int d = int.Parse([rangeDigits[i]]);
                    DrawDigit(graph, digits, W, H, d, x, y, i + o);
                }
            }
        }


        private static void DrawGrid(
            Image graph,
            int graphRes,
            int linesY,
            int linesX,
            int thickness,
            Color color,
            int centerThickness,
            Color centerColor
            )
        {
            //grid lines X
            for (int y = 0; y < graphRes; y++)
            {
                for (int i = 0; i < linesX; i++)
                {
                    int x = (int)(i / (float)linesX * graphRes);

                    for (int j = -(thickness - 1); j <= thickness - 1; j++)
                    {
                        if (x + j < 0 || x + j >= graphRes) continue;

                        if(y <= (graphRes - 1) / 2 + centerThickness * 2 && y >= (graphRes - 1) / 2 - centerThickness * 2)
                        {
                            graph.SetPixel(x + j, y, centerColor);
                        }
                        else
                        {
                            graph.SetPixel(x + j, y, color);
                        }
                    }
                }
            }

            //grid lines Y
            for (int x = 0; x < graphRes; x++)
            {
                for (int i = 0; i < linesY; i++)
                {
                    int y = (int)(i / (float)linesY * (graphRes - 1));

                    for (int j = -(thickness - 1); j <= thickness - 1; j++)
                    {
                        if (y + j < 0 || y + j >= graphRes) continue;
                        if(x <= centerThickness * 2)
                        {
                            graph.SetPixel(x, y + j, centerColor);
                        }
                        else
                        {
                            graph.SetPixel(x, y + j, color);
                        }
                    }
                }
            }


            //center line
            for (int x = 0; x < graphRes; x++)
            {
                for (int i = -(centerThickness - 1); i <= (centerThickness - 1); i++)
                {
                    int y = (graphRes - 1) / 2;
                    if (y + i < 0 || y + i >= graphRes) continue;
                    graph.SetPixel(x, y + i, centerColor);
                }
            }

            //left scale line
            // for (int y = 0; y < graphRes; y++)
            // {
            //     for (int i = -centerThickness; i <= centerThickness; i++)
            //     {
            //         int x = 0;
            //         if (x + i < 0 || x + i >= graphRes) continue;
            //         graph.SetPixel(x + i, y, centerColor);
            //     }
            // }
        }
    }
}
