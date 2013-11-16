﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace IntelOrca.RRHMG.Metro
{
	/// <summary>
	/// Represents a canvas that can contain hexagon shapes representing hexagons.
	/// </summary>
	public sealed class HexagonMap : Canvas
	{
		private const double _initialHexagonSizeFactor = 0.75;

		private Hexagon _showingHexagon;
		private HexagonPattern _hexagonPattern;
		private int _maxLevelsToShow = 5;

		/// <summary>
		/// Event for when a hexagon is tapped.
		/// </summary>
		public event EventHandler<HexagonEventArgs> HexagonTapped;
		
		/// <summary>
		/// Event for when a hexagon is tapped with the right button.
		/// </summary>
		public event EventHandler<HexagonEventArgs> HexagonRightTapped;

		/// <summary>
		/// Gets or sets the top level showing hexagon.
		/// </summary>
		public Hexagon ShowingHexagon
		{
			get { return _showingHexagon; }
			set {
				_showingHexagon = value;
				GenerateHexagonShapes();
			}
		}

		/// <summary>
		/// Gets or sets the hexagon pattern to use.
		/// </summary>
		public HexagonPattern HexagonPattern
		{
			get { return _hexagonPattern; }
			set { _hexagonPattern = value; }
		}

		/// <summary>
		/// Gets or sets the maximum child level of hexagons to show.
		/// </summary>
		public int MaxLevelsToShow
		{
			get { return _maxLevelsToShow; }
			set {
				_maxLevelsToShow = value;
				GenerateHexagonShapes();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HexagonMap"/> class.
		/// </summary>
		public HexagonMap()
		{
			_hexagonPattern = HexagonPattern.Patterns.Get(1);
			this.SizeChanged += (s, e) => GenerateHexagonShapes();
		}

		/// <summary>
		/// Clears all current hexagon shapes and generates hexagon shapes from the head hexagon.
		/// </summary>
		public void GenerateHexagonShapes()
		{
			GenerateHexagonShapes(ActualWidth, ActualHeight);
		}

		/// <summary>
		/// Clears all current hexagon shapes and generates hexagon shapes from the head hexagon.
		/// </summary>
		/// <param name="clientWidth">The rendered width of the view.</param>
		/// <param name="clientHeight">The rendered height of the view.</param>
		public void GenerateHexagonShapes(double clientWidth, double clientHeight)
		{
			// Calculate the the top level hexagon size to cover the entire canvas
			double size = Math.Max(clientWidth, clientHeight) * _initialHexagonSizeFactor;

			// Calculate the centre position of the top level hexagon
			Point centrePosition = new Point(clientWidth / 2.0, clientHeight / 2.0);

			// Get all the non HexagonShape elements so they can be readded after the hexagons
			IEnumerable<UIElement> overlayElements = Children.Where(x => !(x is HexagonShape)).ToArray();

			// Clear the canvas and regenerate the hexagons
			Children.Clear();

			// Add all the hexagons from the top level hexagon
			if (_showingHexagon != null)
				foreach (HexagonShape hex in GenerateHexagonShapes(size, centrePosition, _maxLevelsToShow, _showingHexagon))
					Children.Add(hex);

			// Add the overlay elements
			foreach (UIElement element in overlayElements)
				Children.Add(element);
		}

		/// <summary>
		/// Generates child hexagons of the specified hexagon recursively until the maximum number of child levels shown is
		/// reached or there are no child hexagons.
		/// </summary>
		/// <param name="size">The size of the hexagon shape.</param>
		/// <param name="position">The centre position of the shape of <paramref name="hexagon"/>.</param>
		/// <param name="levels">The current child level relative to the head hexagon.</param>
		/// <param name="hexagon">The parent hexagon of which to generate the child hexagon shapes.</param>
		/// <returns>An enumerable of <see cref="HexagonShape">HexagonShapes</see>.</returns>
		private IEnumerable<HexagonShape> GenerateHexagonShapes(double size, Point position, int levels, Hexagon hexagon)
		{
			// Base case
			if (levels < 0)
				yield break;

			// The child hexagon size calculation
			double nextSize = size * _hexagonPattern.ChildSizeFactor;

			// Get the width and height for this the next size down of this hexagon
			double hexWidth = Hexagon.GetWidth(nextSize);
			double hexHeight = Hexagon.GetHeight(nextSize);

			// Multiply all the child offsets by the calculated hexagon width / height
			var offsets = _hexagonPattern.Offsets.Select(x => new Point(x.X * hexWidth, x.Y * hexHeight)).ToArray();

			// Do not draw any more child hexagons if we have reached the maximum display level or there are no children
			if (levels > 0 && hexagon.Children != null) {
				for (int i = 0; i < hexagon.Children.Length; i++) {
					Point offset = offsets[i];

					// Recursively generate child hexagon shapes
					var hexagonShapes = GenerateHexagonShapes(
						nextSize,
						new Point(position.X + offset.X, position.Y + offset.Y),
						levels - 1,
						hexagon.Children[i]
					);

					// Return all the child hexagon shapes
					foreach (HexagonShape hex in hexagonShapes)
						yield return hex;
				}
			} else {
				// Do not render the shape if it can't be seen
				if (!IsHexagonVisible(position.X, position.Y, size))
					yield break;

				// Return a single hexagon shape for this current hexagon
				yield return GenerateHexagonShape(size, position, hexagon);
			}
		}

		/// <summary>
		/// Generates a single hexagon shape from the specified hexagon.
		/// </summary>
		/// <param name="size">The size of the hexagon shape to generate.</param>
		/// <param name="position">The central position of the hexagon shape to generate.</param>
		/// <param name="hexagon">The hexagon to generate the shape for.</param>
		/// <returns>A <see cref="HexagonShape"/> representing <paramref name="hexagon"/>.</returns>
		private HexagonShape GenerateHexagonShape(double size, Point position, Hexagon hexagon)
		{
			// Create the hexagon shape associated with this hexagon
			var hex = new HexagonShape(hexagon, size);

			// Set the position of the hexagon in the cavas container
			Canvas.SetLeft(hex, position.X - (hex.Width / 2.0));
			Canvas.SetTop(hex, position.Y - (hex.Height / 2.0));

			// Set their tap events
			hex.Tapped += HexagonOnTapped;
			hex.RightTapped += HexagonOnRightTapped;

			return hex;
		}

		/// <summary>
		/// Creates a new <see cref="HexagonEventArgs"/> containing the event data for the specified <see cref="HexagonShape"/>.
		/// </summary>
		/// <param name="shape">The source hexagon shape.</param>
		/// <param name="e">The original event data.</param>
		/// <param name="nextLevelHexagon">
		/// The hexagon one level down from the top level showing hexagon that contains the
		/// source hexagon.
		/// </param>
		/// <returns>A new instance of a <see cref="HexagonEventArgs"/> containing the event data.</returns>
		private HexagonEventArgs GetHexagonEventArgs(HexagonShape shape, RoutedEventArgs e, out Hexagon nextLevelHexagon)
		{
			// Find the number of levels down this hexagon is from the top level showing hexagon
			int levelDepth = 1;
			nextLevelHexagon = shape.Hexagon;
			while (nextLevelHexagon.Parent != _showingHexagon && nextLevelHexagon.Parent != null) {
				nextLevelHexagon = nextLevelHexagon.Parent;
				levelDepth++;
			}

			return new HexagonEventArgs(e, shape, levelDepth);
		}

		/// <summary>
		/// Called when a hexagon shape is tapped.
		/// </summary>
		/// <param name="sender">The <see cref="HexagonShape"/> that was tapped.</param>
		/// <param name="e">The event arguments.</param>
		private void HexagonOnTapped(object sender, TappedRoutedEventArgs e)
		{
			Hexagon nextLevelHexagon;
			HexagonEventArgs hexagonEventArgs = GetHexagonEventArgs((HexagonShape)sender, e, out nextLevelHexagon);

			// Invoke event handler
			if (HexagonTapped != null)
				HexagonTapped.Invoke(this, hexagonEventArgs);

			// Zoom in by one level by default
			if (!hexagonEventArgs.Handled)
				_showingHexagon = nextLevelHexagon;

			// Redisplay
			GenerateHexagonShapes();
		}

		/// <summary>
		/// Called when a hexagon is tapped with the right button.
		/// </summary>
		/// <param name="sender">The <see cref="HexagonShape"/> that was tapped.</param>
		/// <param name="e">The event arguments.</param>
		private void HexagonOnRightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			Hexagon nextLevelHexagon;
			HexagonEventArgs hexagonEventArgs = GetHexagonEventArgs((HexagonShape)sender, e, out nextLevelHexagon);

			// Invoke event handler
			HexagonRightTapped.Invoke(this, hexagonEventArgs);

			// If there is a parent, zoom out to the parent level
			if (!hexagonEventArgs.Handled && _showingHexagon.Parent != null) {
				_showingHexagon = _showingHexagon.Parent;
				GenerateHexagonShapes();
			}
		}

		/// <summary>
		/// Checks whether a hexagon with the specified properties can be seen on the current canvas view.
		/// </summary>
		/// <param name="x">The centre X position of the hexagon.</param>
		/// <param name="y">The centre Y position of the hexagon.</param>
		/// <param name="size">The size of the hexagon.</param>
		/// <returns>True if the hexagon is visible, otherwise false.</returns>
		private bool IsHexagonVisible(double x, double y, double size)
		{
			// Get the hexagon width and height
			double hw = Hexagon.GetWidth(size);
			double hh = Hexagon.GetHeight(size);

			// Get the hexagon bounds and the canvas bounds
			var hexagonBounds = new Rect(x - hw / 2.0, y - hh / 2.0, hw, hh);
			var canvasBounds = new Rect(0, 0, ActualWidth, ActualHeight);

			// Return true if they intersect, i.e. the given hexagon is visible on the canvas
			return canvasBounds.IntersectsWith(hexagonBounds);
		}
	}
}