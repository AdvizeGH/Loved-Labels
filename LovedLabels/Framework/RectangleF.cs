namespace LovedLabels.Framework
{
    public class RectangleF
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Contains(float xPos, float yPos)
        {
            return this.X <= xPos && xPos < this.X + this.Width && this.Y <= yPos && yPos < this.Y + this.Height;
        }
    }
}
