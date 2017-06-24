﻿// Copyright © 2017 Paddy Xu
// 
// This file is part of QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace QuickLook.Plugin.PDFViewer
{
    public class Plugin : IViewer
    {
        private PdfViewerControl _pdfControl;

        public int Priority => int.MaxValue;
        public bool AllowsTransparency => true;

        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            if (File.Exists(path) && Path.GetExtension(path).ToLower() == ".pdf")
                return true;

            //using (var br = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            //{
            //    return Encoding.ASCII.GetString(br.ReadBytes(4)) == "%PDF";
            //}
            return false;
        }

        public void Prepare(string path, ContextObject context)
        {
            var desiredSize = PdfViewerControl.GetDesiredControlSizeByFirstPage(path);

            context.SetPreferredSizeFit(desiredSize, 0.8);
        }

        public void View(string path, ContextObject context)
        {
            _pdfControl = new PdfViewerControl();
            context.ViewerContent = _pdfControl;

            Exception exception = null;

            _pdfControl.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    _pdfControl.LoadPdf(path);

                    context.Title = $"{Path.GetFileName(path)} (1 / {_pdfControl.TotalPages})";
                    _pdfControl.CurrentPageChanged += (sender2, e2) => context.Title =
                        $"{Path.GetFileName(path)} ({_pdfControl.CurrentPage + 1} / {_pdfControl.TotalPages})";
                    context.IsBusy = false;
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }), DispatcherPriority.Loaded).Wait();

            if (exception != null)
                ExceptionDispatchInfo.Capture(exception).Throw();
        }

        public void Cleanup()
        {
            GC.SuppressFinalize(this);

            _pdfControl?.Dispose();
            _pdfControl = null;
        }

        ~Plugin()
        {
            Cleanup();
        }
    }
}