﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace IntelOrca.RRHMG
{
	/// <summary>
	/// Represents a map cell hexagon that can contain child hexagons and be a child of a parent hexagon.
	/// </summary>
    public class Hexagon
    {
		/// <summary>
		/// The parent hexagon.
		/// </summary>
		public Hexagon Parent { get; set; }

		/// <summary>
		/// Child hexagons inside this one.
		/// </summary>
		public Hexagon[] Children { get; set; }

		/// <summary>
		/// The terrain information for this hexagon.
		/// </summary>
		public TerrainInfo TerrainInfo { get; set; }

		/// <summary>
		/// Gets the child hexagons from this hexagon's parent excluding this hexagon.
		/// </summary>
		public IEnumerable<Hexagon> Siblings
		{
			get
			{
				if (Parent == null)
					return Enumerable.Empty<Hexagon>();
				return Parent.Children.Where(x => x != this);
			}
		}

		/// <summary>
		/// Initialises a new instance of the <see cref="Hexagon"/> class.
		/// </summary>
		/// <param name="terrainInfo">The terrain info.</param>
		public Hexagon(TerrainInfo terrainInfo)
		{
			TerrainInfo = terrainInfo;
		}

		/// <summary>
		/// Initialises a new instance of the <see cref="Hexagon"/> class.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="terrainInfo">The terrain info.</param>
		public Hexagon(Hexagon parent, TerrainInfo terrainInfo)
		{
			Parent = parent;
			TerrainInfo = terrainInfo;
		}

		/// <summary>
		/// Generates child hexagons with terrain based on the terrain of this hexagon.
		/// </summary>
		/// <param name="random"></param>
		public void GenerateChildren(Random random, HexagonPattern pattern)
		{
			int numChildHexagonsToGenerate = pattern.Offsets.Count;
			int centralHexagonIndex = numChildHexagonsToGenerate / 2;

			var children = new Hexagon[numChildHexagonsToGenerate];

			// CHECK this is the same reference to parent terrain information
			children[centralHexagonIndex] = new Hexagon(this, TerrainInfo);

			// Generate the hexagons surrounding the central hexagon
			for (int i = 0; i < numChildHexagonsToGenerate; i++)
				if (i != centralHexagonIndex)
					children[i] = new Hexagon(this, TerrainInfo.FromHexagon(this, random));

			Children = children;
		}

		#region Static methods

		/// <summary>
		/// The offsets of the hexagon polygon points in units of hexagon width / height relative to the position of the
		/// hexagon.
		/// </summary>
		public static IReadOnlyList<Point> PointSizeOffsets = new Point[] {
			new Point(-0.50,  0.0),
			new Point(-0.25, -0.5),
			new Point( 0.25, -0.5),
			new Point( 0.50,  0.0),
			new Point( 0.25,  0.5),
			new Point(-0.25,  0.5),
		};

		/// <summary>
		/// Gets the width of a hexagon given its size.
		/// </summary>
		/// <param name="size">The size of the hexagon.</param>
		public static double GetWidth(double size)
		{
			return size * 2.0;
		}

		/// <summary>
		/// Gets the height of a hexagon given its size.
		/// </summary>
		/// <param name="size">The size of the hexagon.</param>
		public static double GetHeight(double size)
		{
			return size * Math.Sqrt(3.0);
		}

		#endregion
	}
}
