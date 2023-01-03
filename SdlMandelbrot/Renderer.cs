using SDL2;
using System.Drawing;

namespace SdlMandelbrot
{
    internal class Renderer
    {
        private IntPtr _renderer;
        private IntPtr _window;
        private bool _running;

        private int _width;
        private int _height;

        private Color[] _pixels;

        private const byte MAX_ITERATIONS = 0xFF;
        private Color[] _colours;

        private int _currentX;
        private int _currentY;

        private void initVideo(int videoWidth, int videoHeight)
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            {
                Console.WriteLine($"Init error: {SDL.SDL_GetError()}");
            }

            _window = SDL.SDL_CreateWindow("Mandelbrot", SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, videoWidth, videoHeight, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

            if (_window == IntPtr.Zero)
            {
                Console.WriteLine($"Window error: {SDL.SDL_GetError()}");
            }

            _renderer = SDL.SDL_CreateRenderer(_window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

            if (_renderer == IntPtr.Zero)
            {
                Console.WriteLine($"Renderer error: {SDL.SDL_GetError()}");
            }
        }

        private void checkEvents()
        {
            while(SDL.SDL_PollEvent(out SDL.SDL_Event e) == 1)
            {
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        _running = false;
                        break;
                }
            }
        }

        private void clearScreen()
        {
            if (SDL.SDL_SetRenderDrawColor(_renderer, 0, 0, 0, 255) < 0)
            {
                Console.WriteLine($"Colour error: {SDL.SDL_GetError()}");
            }

            if (SDL.SDL_RenderClear(_renderer) < 0)
            {
                Console.WriteLine($"Clear error: {SDL.SDL_GetError()}");
            }
        }

        private void render()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var c = _pixels[x + (_width * y)];
                    SDL.SDL_SetRenderDrawColor(_renderer, c.R, c.G, c.B, c.A);
                    SDL.SDL_RenderDrawPoint(_renderer, x, y);
                }
            }
        }

        public void Run(int videoWidth, int videoHeight)
        {
            _width = videoWidth;
            _height = videoHeight;

            _pixels = new Color[_width * _height];

            generateColours();

            _running = true;

            initVideo(videoWidth, videoHeight);

            new Thread(worker).Start();

            while(_running)
            {
                checkEvents();

                clearScreen();

                render();

                SDL.SDL_RenderPresent(_renderer);
            }

            SDL.SDL_DestroyRenderer(_renderer);
            SDL.SDL_DestroyWindow(_window);
            SDL.SDL_Quit();
        }

        private void worker()
        {
            double mleft = -2;
            double mright = 1;
            double mtop = -1.5;
            double mbottom = 1.5;
            double mwidth = mright - mleft;
            double mheight = mbottom - mtop;

            double magnification = 1.0;
            double bwidth = mwidth / magnification;
            double bheight = mheight / magnification;

            int magnificationblockx = 0;
            int magnificationblocky = 0;

            double cleft = mleft + (bwidth * magnificationblockx);
            double cright = mleft + (bwidth * (magnificationblockx + 1));
            double ctop = mtop + (bheight * magnificationblocky);
            double cbottom = mtop + (bheight * (magnificationblocky + 1));

            processPixels(cleft, cright, ctop, cbottom);
        }

        private void processPixels(double left, double right, double top, double bottom)
        {
            double width = right - left;
            double height = bottom - top;

            double hstep = width / _width;
            double vstep = height / _height;

            _currentY = 0;
            for (double y = top; _currentY < _height && _running; y+=vstep)
            {
                _currentX = 0;
                for (double x = left; _currentX < _width && _running; x+=hstep)
                {
                    _pixels[_currentX + (_width * _currentY)] = processPixel(x, y);
                    _currentX++;
                }
                _currentY++;
            }
        }

        private Color processPixel(double x, double y)
        {
            double x1, x0;
            x1 = x0 = x;
            double y1, y0;
            y1 = y0 = y;
            int iteration = 0;

            while (x1 * x1 + y1 * y1 <= (2 * 2) && iteration < MAX_ITERATIONS)
            {
                double xtemp = x1 * x1 - y1 * y1 + x0;
                y1 = 2 * x1 * y1 + y0;
                x1 = xtemp;
                iteration++;
            }

            if (iteration == MAX_ITERATIONS)
            {
                return Color.Black;
            }

            return _colours[iteration];
        }

        private void generateColours()
        {
            _colours = new Color[MAX_ITERATIONS];
            int increment = 0;
            for (byte r = 0x00; r < 0xff && increment < MAX_ITERATIONS; r += 25)
            {
                for (byte g = 0x00; g < 0xff && increment < MAX_ITERATIONS; g += 25)
                {
                    for (byte b = 0x00; b < 0xff && increment < MAX_ITERATIONS; b += 25)
                    {
                        _colours[increment] = Color.FromArgb(r, g, b);
                        increment++;
                    }
                }
            }
        }
    }
}
