﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using JollyBit.BS.Core.Utility;

namespace JollyBit.BS.Client.Rendering
{
    public interface ITextureAtlasFactory
    {
        ITextureAtlas CreateTextureAtlas(int atlasWidth, Size subImageSize, int numMipmapLevels);
    }

    public interface ITextureAtlas : IRenderable
    {
        /// <summary>
        /// Adds a sub image to the texture atlas. If the sub image has already been added it is
        /// not added agian.
        /// </summary>
        /// <param name="bitmap">The bitmap to add as a sub image.</param>
        /// <returns>A texture reference allowing the use of the sub image.</returns>
        ITextureReference AddSubImage(IBitmap bitmap);
    }

    public class TextureAtlasFactory : ITextureAtlasFactory
    {
        public ITextureAtlas CreateTextureAtlas(int atlasWidth, Size subImageSize, int numMipmapLevels)
        {
            return new TextureAtlas(atlasWidth, subImageSize, numMipmapLevels);
        }
    }

    public class TextureAtlas : ITextureAtlas
    {
        public readonly Size SubImageSize;
        public readonly int NumberOfSubImages;
        public readonly Bitmap Texture;
        public readonly int NumMipmapLevels;
        private int _currentSubImage = 0;
        public readonly int BorderSize;
        private readonly Dictionary<string, ITextureReference> _refCache = new Dictionary<string, ITextureReference>();
        public TextureAtlas(int atlasWidth, Size subImageSize, int numMipmapLevels)
        {
            BorderSize = numMipmapLevels;
            SubImageSize.Width = subImageSize.Width - numMipmapLevels * 2;
			SubImageSize.Height = subImageSize.Height - numMipmapLevels * 2;
            NumberOfSubImages = atlasWidth / subImageSize.Width;
            NumMipmapLevels = numMipmapLevels;
            //Create empty white bitmap to hold atlas
            Texture = new Bitmap(atlasWidth, atlasWidth);
            using (Graphics graphics = Graphics.FromImage(Texture))
            {
                graphics.FillRectangle(new SolidBrush(Color.White), new Rectangle(0, 0, Texture.Width, Texture.Height));
            }
        }

		private unsafe void addBorder(ref Rectangle rect) {
			Rectangle editing = new Rectangle(
				rect.X - BorderSize,
			    rect.Y - BorderSize,
			    rect.Width + 2 * BorderSize,
			    rect.Height + 2 * BorderSize
			);
			
			BitmapData currentData = Texture.LockBits(editing, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			uint* scan0 = (uint *)(currentData.Scan0);
			
			// Handle top
			uint *fromPtr = scan0 + editing.Width * BorderSize + BorderSize;
			for(uint i = 0; i < BorderSize; i++) {
				uint *toPtr = scan0 + BorderSize + i * editing.Width;
				for(uint j = 0; j < rect.Width; j++) {
					*(toPtr + j) = *(fromPtr + j);
				}
			}
			
			// Handle bottom
			fromPtr = scan0 + editing.Width * (rect.Height + BorderSize - 1) + BorderSize;
			for(uint i = 0; i < BorderSize; i++) {
				uint *toPtr = scan0 + editing.Width * (rect.Height + BorderSize) + BorderSize + i * editing.Width;
				for(uint j = 0; j < rect.Width; j++) {
					*(toPtr + j) = *(fromPtr + j);
				}
			}
			
			// Handle left
			for(uint i = 0; i < rect.Height; i++) {
				fromPtr = scan0 + editing.Width * (BorderSize + i) + BorderSize;
				uint *toPtr = scan0 + editing.Width * (BorderSize + i);
				for(uint j = 0; j < BorderSize; j++) {
					*(toPtr + j) = *(fromPtr);
				}
			}
			
			// Handle right
			for(uint i = 0; i < rect.Height; i++) {
				fromPtr = scan0 + editing.Width * (BorderSize + i + 1) - BorderSize - 1;
				uint *toPtr = scan0 + editing.Width * (BorderSize + i) + rect.Width + BorderSize;
				for(uint j = 0; j < BorderSize; j++) {
					*(toPtr + j) = *(fromPtr);
				}
			}
			
			/// Ignored corners because there were no visable rendering artifacts		
			
			Texture.UnlockBits(currentData);
			
			// To save texture atlas to file after add, uncomment the following line.
			//Texture.Save(string.Format("/tmp/texture-{0}.bmp",System.DateTime.Now.Millisecond));
		}
		
        public ITextureReference AddSubImage(IBitmap bitmap)
        {
            //Check if already added
            ITextureReference textureRef;
            if (_refCache.TryGetValue(bitmap.UniqueId, out textureRef))
            {
                return textureRef;
            }

            //Clean up old texture object
            if (_textureObject != null)
            {
                _textureObject.Dispose();
                _textureObject = null;
            }

            //Add sub image
            Rectangle rect = new Rectangle(
                (_currentSubImage % NumberOfSubImages) * (SubImageSize.Width) + BorderSize, // X
                (_currentSubImage / NumberOfSubImages) * (SubImageSize.Height) + BorderSize, // Y
                SubImageSize.Width - 2 * BorderSize, // Width
                SubImageSize.Height - 2 * BorderSize); // Height

            using (Graphics g = Graphics.FromImage(Texture))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				//Console.WriteLine("Writing subimage {0} (sized {1}) to area {2}.",_currentSubImage,SubImageSize,rect);
                g.DrawImage(bitmap.Bitmap, rect); 
            }
			
			// Extend the last pixel out past the border
			addBorder(ref rect);
			
            _currentSubImage++;
            //Create ITextureReference
            RectangleF location = new RectangleF(rect.X / (float)Texture.Width, rect.Y / (float)Texture.Height,
                rect.Width / (float)Texture.Width, rect.Height / (float)Texture.Height);
            textureRef = new TextureReference(location, new Action(Render));
            _refCache.Add(bitmap.UniqueId, textureRef);
            return textureRef;
        }

        private GLTextureObject _textureObject = null;
        public void Render()
        {
            if (_textureObject == null)
            {
                _textureObject = new GLTextureObject(Texture, NumMipmapLevels);
            }
            _textureObject.Render();
        }
    }
}
