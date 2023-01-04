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

        private Color[] _colours;

        private int _currentPixelX;
        private int _currentPixelY;

        private int _maxIterations = 256;
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
                case SDL.SDL_Keycode.SDLK_q:
                    if (_working == false)
                    {
                        _maxIterations *= 2;
                        launchWorker();
                    }
                    break;
                case SDL.SDL_Keycode.SDLK_a:
                    if (_working == false)
                    {
                        _maxIterations /= 2;
                        launchWorker();
                    }
                    break;
            }
            setWindowTitle();
        }

        private void setWindowTitle()
        {
            SDL.SDL_SetWindowTitle(_window, $"Mandelbrot [Mag:{_magnification} Block:{_magnificationblockx},{_magnificationblocky} Iterations:{_maxIterations}]");
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
                    if (x == _currentPixelX && y == _currentPixelY)
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

            double fractal_left = -2;
            double fractal_right = 1;
            double fractal_top = -1.5;
            double fractal_bottom = 1.5;
            double fractal_width = fractal_right - fractal_left;
            double fractal_height = fractal_bottom - fractal_top;

            double viewable_width = fractal_width / _magnification;
            double viewable_height = fractal_height / _magnification;

            double display_left = fractal_left + (viewable_width * _magnificationblockx);
            double display_right = fractal_left + (viewable_width * (_magnificationblockx + 1));
            double display_top = fractal_top + (viewable_height * _magnificationblocky);
            double display_bottom = fractal_top + (viewable_height * (_magnificationblocky + 1));

            generateColours();

            processPixels(display_left, display_right, display_top, display_bottom);

            _working = false;
        }

        private void processPixels(double left, double right, double top, double bottom)
        {
            double width = right - left;
            double height = bottom - top;

            double hstep = width / _width;
            double vstep = height / _height;

            _currentPixelY = 0;
            for (double pixelYValue = top; _currentPixelY < _height && _running; pixelYValue+=vstep)
            {
                _currentPixelX = 0;
                for (double xpixelXValue = left; _currentPixelX < _width && _running; xpixelXValue += hstep)
                {
                    _pixels[_currentPixelX + (_width * _currentPixelY)] = processPixel(xpixelXValue, pixelYValue);
                    _currentPixelX++;
                }
                _currentPixelY++;
            }
        }

        private Color processPixel(double x, double y)
        {
            double x1, x0;
            x1 = x0 = x;
            double y1, y0;
            y1 = y0 = y;
            int iteration = 0;

            while (x1 * x1 + y1 * y1 <= (2 * 2) && iteration < _maxIterations)
            {
                double xtemp = x1 * x1 - y1 * y1 + x0;
                y1 = 2 * x1 * y1 + y0;
                x1 = xtemp;
                iteration++;
            }

            if (iteration == _maxIterations)
            {
                return Color.Black;
            }

            return _colours[iteration];
        }

        private void generateColours()
        {
            double max = 0xFF * 0xFF * 0xFF;
            var step = (int)Math.Ceiling(max / _maxIterations);

            _colours = new Color[_maxIterations];

            int inc = 0;
            for (int i = 0; i < max; i+= step)
            {
                var r = (i >> 16) & 0x000000FF;
                var g = (i >> 8) & 0x000000FF;
                var b = (i >> 0) & 0x000000FF;
                _colours[inc] = Color.FromArgb(r, g, b);
                inc++;
            }
        }
    }
}
