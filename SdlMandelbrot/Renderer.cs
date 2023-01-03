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

        private double _magnification = 1.0;
        private bool _working;
        private int _magnificationblockx = 0;
        private int _magnificationblocky = 0;

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
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                        checkKey(e.key);
                        break;
                }
            }
        }

        private void checkKey(SDL.SDL_KeyboardEvent e)
        {
            switch (e.keysym.sym)
            {
                case SDL.SDL_Keycode.SDLK_ESCAPE:
                    _running = false;
                    break;
                case SDL.SDL_Keycode.SDLK_PAGEUP:
                    if (_working == false)
                    {
                        _magnification += 0.5;
                        launchWorker();
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_PAGEDOWN:
                    if (_working == false)
                    {
                        _magnification -= 0.5;
                        launchWorker();
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_DOWN:
                    if (_working == false)
                    {
                        _magnificationblocky += 1;
                        launchWorker();
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_UP:
                    if (_working == false)
                    {
                        _magnificationblocky -= 1;
                        launchWorker();
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_RIGHT:
                    if (_working == false)
                    {
                        _magnificationblockx += 1;
                        launchWorker();
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_LEFT:
                    if (_working == false)
                    {
                        _magnificationblockx -= 1;
                        launchWorker();
                    }
                    break;
            }
            setWindowTitle();
        }

        private void setWindowTitle()
        {
            SDL.SDL_SetWindowTitle(_window, $"Mandelbrot {_magnification} {_magnificationblockx},{_magnificationblocky}");
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
                    if (x == _currentX && y == _currentY)
                    {
                        SDL.SDL_SetRenderDrawColor(_renderer, 0xFF, 0xFF, 0xFF, 0xFF);
                    }
                    else
                    {
                        var c = _pixels[x + (_width * y)];
                        SDL.SDL_SetRenderDrawColor(_renderer, c.R, c.G, c.B, c.A);
                    }
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

            setWindowTitle();

            launchWorker();

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

        private void launchWorker()
        {
            if (_working == false)
            {
                new Thread(worker).Start();
            }
        }

        private void worker()
        {
            _working = true;

            double mleft = -2;
            double mright = 1;
            double mtop = -1.5;
            double mbottom = 1.5;
            double mwidth = mright - mleft;
            double mheight = mbottom - mtop;

            double bwidth = mwidth / _magnification;
            double bheight = mheight / _magnification;

            double cleft = mleft + (bwidth * _magnificationblockx);
            double cright = mleft + (bwidth * (_magnificationblockx + 1));
            double ctop = mtop + (bheight * _magnificationblocky);
            double cbottom = mtop + (bheight * (_magnificationblocky + 1));

            processPixels(cleft, cright, ctop, cbottom);

            _working = false;
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
