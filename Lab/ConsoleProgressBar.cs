using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab
{
    class ConsoleProgressBar
    {
        static readonly string filling = "-#"; //"⠀⠁⠃⠇⠏⠟⠿";
        static readonly int stepCount = filling.Length - 1;
        static readonly double step = 1.0 / stepCount;
        static readonly int length = 20;
        readonly int Row;
        StringBuilder BackBuffer = new StringBuilder();
        bool reprinting = true;

        public ConsoleProgressBar(bool reprinting = false)
        {
            this.reprinting = reprinting;
            Row = Console.CursorTop;
        }

        public void Write(double progress, string additional)
        {
            double percent = progress * 100.0;
            if (progress < 0.0)
                progress = 0.0;
            if (progress > 1.0)
                progress = 1.0;
            if (additional.Length > 0)
                reprinting = false;

            StringBuilder bar = new StringBuilder("[");
            double cells = progress * length;
            int filled = (int)Math.Floor(cells);
            for (int i = 0; i < filled; i++)
                bar.Append(filling[stepCount]);
            for (int i = filled; i < length; i++)
                bar.Append(filling[0]);

            double remainder = cells - filled;
            if (remainder > step)
                bar[filled] = filling[(int)Math.Floor(remainder * stepCount)];

            bar.Append("] ");
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            bar.Append(percent.ToString("00.0", culture));
            bar.Append("%");
            if (!reprinting)
                bar.Append("\n");
            bar.Append(additional);

            if (reprinting)
                Console.WriteLine(BackBuffer);
            else
                Console.SetCursorPosition(0, Row); // TODO fix flicker: https://stackoverflow.com/questions/5435460/console-application-how-to-update-the-display-without-flicker
            Console.Write(bar);
            if (BackBuffer.Length > bar.Length)
                BackBuffer.Remove(bar.Length, BackBuffer.Length - bar.Length);
            if (BackBuffer.Length < bar.Length)
                BackBuffer.Append('\r', bar.Length - BackBuffer.Length);
        }
    }
}
