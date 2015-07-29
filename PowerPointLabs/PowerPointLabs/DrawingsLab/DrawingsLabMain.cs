﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using PowerPointLabs.DataSources;
using PowerPointLabs.Models;
using PPExtraEventHelper;
using Shape = Microsoft.Office.Interop.PowerPoint.Shape;
using Graphics = PowerPointLabs.Utils.Graphics;

namespace PowerPointLabs.DrawingsLab
{
    internal class DrawingsLabMain
    {
        private static Dictionary<Native.VirtualKey, ControlGroup> ControlGroups = new Dictionary<Native.VirtualKey, ControlGroup>();  

        private struct ControlGroup
        {
            public readonly int SlideId;
            public readonly HashSet<int> ShapeIds;

            public ControlGroup(int slideId, HashSet<int> shapeIds)
            {
                SlideId = slideId;
                ShapeIds = shapeIds;
            }
        }


        public static DrawingsLabDataSource DataSource
        {
            get { return DrawingsPaneWPF.dataSource; }
        }

        #region API
        public static void SwitchToLineTool()
        {
            // This should trigger the line tool.
            // see https://github.com/PowerPointLabs/powerpointlabs/blob/master/PowerPointLabs/PowerPointLabs/ThisAddIn.cs#L1381
            //TODO: Placeholder code. This just triggers the property window.
            Native.SendMessage(
                Process.GetCurrentProcess().MainWindowHandle,
                (uint)Native.Message.WM_COMMAND,
                new IntPtr(0x8F),
                IntPtr.Zero
                );
        }

        public static void HideTool()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            foreach (var shape in selection.ShapeRange.Cast<Shape>())
            {
                shape.Visible = MsoTriState.msoFalse;
            }
        }

        public static void ShowAllTool()
        {
            var shapes = PowerPointCurrentPresentationInfo.CurrentSlide.Shapes;

            foreach (var shape in shapes.Cast<Shape>())
            {
                shape.Visible = MsoTriState.msoTrue;
            }
        }

        public static void CloneTool()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            PowerPointCurrentPresentationInfo.CurrentSlide.CopyShapesToSlide(selection.ShapeRange);
        }

        public static void MultiCloneExtendTool()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;
            var shapeList = selection.ShapeRange.Cast<Shape>().ToList();

            if (shapeList.Count % 2 != 0)
            {
                Error("There must be two sets of shapes selected.");
                return;
            }

            int clones = DrawingsLabDialogs.ShowNumericDialog("Number of copies:", "Multi-Clone") - 1;
            if (clones <= 0) return;

            int midpoint = shapeList.Count / 2;
            for (int i = 0; i < shapeList.Count / 2; ++i)
            {
                // Do the cloning for every pair of shapes (i, midpoint+i)
                var firstShape = shapeList[i];
                var secondShape = shapeList[midpoint + i];

                for (int j = 0; j < clones; ++j)
                {
                    var newShape = firstShape.Duplicate()[1];
                    int index = j + 1;

                    newShape.Left = secondShape.Left + (secondShape.Left - firstShape.Left) * index;
                    newShape.Top = secondShape.Top + (secondShape.Top - firstShape.Top) * index;
                    newShape.Rotation = secondShape.Rotation + (secondShape.Rotation - firstShape.Rotation) * index;
                }
            }
        }

        public static void MultiCloneBetweenTool()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;
            var shapeList = selection.ShapeRange.Cast<Shape>().ToList();

            if (shapeList.Count % 2 != 0)
            {
                Error("There must be two sets of shapes selected.");
                return;
            }

            int clones = DrawingsLabDialogs.ShowNumericDialog("Number of copies:", "Multi-Clone") - 1;
            if (clones <= 0) return;

            int divisions = clones + 1;

            int midpoint = shapeList.Count / 2;
            for (int i = 0; i < shapeList.Count / 2; ++i)
            {
                // Do the cloning for every pair of shapes (i, midpoint+i)
                var firstShape = shapeList[i];
                var lastShape = shapeList[midpoint + i];

                for (int j = 0; j < clones; ++j)
                {
                    var newShape = firstShape.Duplicate()[1];
                    int index = j + 1;

                    newShape.Left = firstShape.Left + (lastShape.Left - firstShape.Left) / divisions * index;
                    newShape.Top = firstShape.Top + (lastShape.Top - firstShape.Top) / divisions * index;
                    newShape.Rotation = firstShape.Rotation + (lastShape.Rotation - firstShape.Rotation) / divisions * index;
                    Graphics.MoveZUntilBehind(newShape, lastShape);
                }
            }
        }

        public static void SendBackward()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            selection.ShapeRange.ZOrder(MsoZOrderCmd.msoSendBackward);
        }

        public static void BringForward()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            selection.ShapeRange.ZOrder(MsoZOrderCmd.msoBringForward);
        }

        public static void SendToBack()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            selection.ShapeRange.ZOrder(MsoZOrderCmd.msoSendToBack);
        }

        public static void BringToFront()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            selection.ShapeRange.ZOrder(MsoZOrderCmd.msoBringToFront);
        }

        public static void SendBehindShape()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            var shapes = selection.ShapeRange.Cast<Shape>().ToList();
            if (shapes.Count < 2)
            {
                Error("Please select at least two shapes");
                return;
            }
            var shapeToMoveBehind = shapes.Last();
            shapes.RemoveAt(shapes.Count - 1);

            Graphics.SortByZOrder(shapes);
            shapes.Reverse();
            foreach (var shape in shapes)
            {
                Graphics.MoveZUntilBehind(shape, shapeToMoveBehind);
            }
        }

        public static void BringInFrontOfShape()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            var shapes = selection.ShapeRange.Cast<Shape>().ToList();
            if (shapes.Count < 2)
            {
                Error("Please select at least two shapes");
                return;
            }
            var shapeToMoveInFront = shapes.Last();
            shapes.RemoveAt(shapes.Count - 1);

            Graphics.SortByZOrder(shapes);
            foreach (var shape in shapes)
            {
                Graphics.MoveZUntilInFront(shape, shapeToMoveInFront);
            }
        }

        public static void RecordDisplacement()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            var shapes = selection.ShapeRange;
            if (shapes.Count != 2)
            {
                Error("Please select a start shape and an end shape");
                return;
            }
            var firstShape = shapes[1];
            var secondShape = shapes[2];

            DataSource.ShiftValueX = GetX(secondShape) - GetX(firstShape);
            DataSource.ShiftValueY = GetY(secondShape) - GetY(firstShape);
            DataSource.ShiftValueRotation = secondShape.Rotation - firstShape.Rotation;
        }

        public static void ApplyDisplacement()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;
            foreach (var shape in selection.ShapeRange.Cast<Shape>())
            {
                if (DataSource.ShiftIncludePositionX)
                {
                    SetX(shape, GetX(shape) + DataSource.ShiftValueX);
                }
                if (DataSource.ShiftIncludePositionY)
                {
                    SetY(shape, GetY(shape) + DataSource.ShiftValueY);
                }
                if (DataSource.ShiftIncludeRotation)
                {
                    shape.Rotation += DataSource.ShiftValueRotation;
                }
            }
        }

        public static void RecordPosition()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            var shapes = selection.ShapeRange;
            if (shapes.Count != 1)
            {
                Error("Please select a single shape");
                return;
            }
            var shape = shapes[1];

            DataSource.SavedValueX = GetX(shape);
            DataSource.SavedValueY = GetY(shape);
            DataSource.SavedValueRotation = shape.Rotation;
        }

        public static void ApplyPosition()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            foreach (var shape in selection.ShapeRange.Cast<Shape>())
            {
                if (DataSource.SavedIncludePositionX)
                {
                    SetX(shape, DataSource.SavedValueX);
                }
                if (DataSource.SavedIncludePositionY)
                {
                    SetY(shape, DataSource.SavedValueY);
                }
                if (DataSource.SavedIncludeRotation)
                {
                    shape.Rotation = DataSource.SavedValueRotation;
                }
            }
        }


        public static void RecordFormat()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            var shapes = selection.ShapeRange;
            if (shapes.Count != 1)
            {
                Error("Please select a single shape");
                return;
            }
            var shape = shapes[1];

            DataSource.FormatFillColor = shape.Fill.ForeColor.RGB;
            DataSource.FormatLineColor = shape.Line.ForeColor.RGB;
            DataSource.FormatLineWeight = shape.Line.Visible == MsoTriState.msoTrue ? shape.Line.Weight : 0;
        }

        public static void ApplyFormat()
        {
            var selection = PowerPointCurrentPresentationInfo.CurrentSelection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            foreach (var shape in selection.ShapeRange.Cast<Shape>())
            {
                if (DataSource.FormatIncludeFillColor)
                {
                    try
                    {
                        shape.Fill.ForeColor.RGB = DataSource.FormatFillColor;
                    }
                    catch (ArgumentException e)
                    {
                        // ArgumentException is thrown if the shape does not have this property.
                    }
                }
                if (DataSource.FormatIncludeLineColor)
                {
                    try
                    {
                        shape.Line.ForeColor.RGB = DataSource.FormatLineColor;
                    }
                    catch (ArgumentException e)
                    {
                        // ArgumentException is thrown if the shape does not have this property.
                    }
                }
                if (DataSource.FormatIncludeLineWeight)
                {
                    if (DataSource.FormatLineWeight <= 0)
                    {
                        shape.Line.Visible = MsoTriState.msoFalse;
                    }
                    else
                    {
                        shape.Line.Visible = MsoTriState.msoTrue;
                        try
                        {
                            shape.Line.Weight = DataSource.FormatLineWeight;
                        }
                        catch (ArgumentException e)
                        {
                            // ArgumentException is thrown if the value is out of range.
                        }
                    }
                }
            }
        }

        public static void SetControlGroup(Native.VirtualKey key)
        {
            if (!Native.IsNumberKey(key)) return;

            var selection = Globals.ThisAddIn.Application.ActiveWindow.Selection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            var shapes = new HashSet<int>(selection.ShapeRange.Cast<Shape>().Select(shape => shape.Id));
            var slideId = PowerPointCurrentPresentationInfo.CurrentSlide.ID;

            ControlGroups[key] = new ControlGroup(slideId, shapes);
        }

        public static void SelectControlGroup(Native.VirtualKey key)
        {
            if (!Native.IsNumberKey(key)) return;

            var selection = Globals.ThisAddIn.Application.ActiveWindow.Selection;
            if (selection.Type == PpSelectionType.ppSelectionSlides) return;

            var currentSlide = PowerPointCurrentPresentationInfo.CurrentSlide;
            if (!ControlGroups.ContainsKey(key)) return;

            var controlGroup = ControlGroups[key];
            var targetSlide = PowerPointPresentation.Current.Slides.FirstOrDefault(slide => slide.ID == controlGroup.SlideId);
            if (targetSlide == null) return;


            targetSlide.GetNativeSlide().Select();

            Globals.ThisAddIn.Application.ActiveWindow.Selection.Unselect();
            var shapeIds = controlGroup.ShapeIds;
            currentSlide.Shapes.Cast<Shape>()
                               .Where(shape => shapeIds.Contains(shape.Id))
                               .ToList()
                               .ForEach(shape => shape.Select(MsoTriState.msoFalse));
        }


        public static void SelectAllOfType()
        {
            var selection = Globals.ThisAddIn.Application.ActiveWindow.Selection;
            if (selection.Type != PpSelectionType.ppSelectionShapes) return;

            var selectedShapeTypes = new HashSet<MsoAutoShapeType>(selection.ShapeRange.Cast<Shape>().Select(shape => shape.AutoShapeType));

            PowerPointCurrentPresentationInfo.CurrentSlide.Shapes.Cast<Shape>()
                                                                 .Where(shape => selectedShapeTypes.Contains(shape.AutoShapeType))
                                                                 .ToList()
                                                                 .ForEach(shape => shape.Select(MsoTriState.msoFalse));
        }

        #endregion

        #region Convenience Functions
        public static float GetX(Shape shape)
        {
            switch (DataSource.AnchorHorizontal)
            {
                case DrawingsLabDataSource.Horizontal.Left:
                    return shape.Left;
                case DrawingsLabDataSource.Horizontal.Center:
                    return Graphics.GetMidpointX(shape);
                case DrawingsLabDataSource.Horizontal.Right:
                    return Graphics.GetRight(shape);
            }
            throw new ArgumentOutOfRangeException();
        }

        public static void SetX(Shape shape, float value)
        {
            switch (DataSource.AnchorHorizontal)
            {
                case DrawingsLabDataSource.Horizontal.Left:
                    shape.Left = value;
                    return;
                case DrawingsLabDataSource.Horizontal.Center:
                    Graphics.SetMidpointX(shape, value);
                    return;
                case DrawingsLabDataSource.Horizontal.Right:
                    Graphics.SetRight(shape, value);
                    return;
            }
            throw new ArgumentOutOfRangeException();
        }

        public static float GetY(Shape shape)
        {
            switch (DataSource.AnchorVertical)
            {
                case DrawingsLabDataSource.Vertical.Top:
                    return shape.Top;
                case DrawingsLabDataSource.Vertical.Middle:
                    return Graphics.GetMidpointY(shape);
                case DrawingsLabDataSource.Vertical.Bottom:
                    return Graphics.GetBottom(shape);
            }
            throw new ArgumentOutOfRangeException();
        }

        public static void SetY(Shape shape, float value)
        {
            switch (DataSource.AnchorVertical)
            {
                case DrawingsLabDataSource.Vertical.Top:
                    shape.Top = value;
                    return;
                case DrawingsLabDataSource.Vertical.Middle:
                    Graphics.SetMidpointY(shape, value);
                    return;
                case DrawingsLabDataSource.Vertical.Bottom:
                    Graphics.SetBottom(shape, value);
                    return;
            }
            throw new ArgumentOutOfRangeException();
        }

        #endregion


        #region Utility Functions
        private static void Error(string message)
        {
            MessageBox.Show(message, "Error");
            // for now do nothing.
        }

        #endregion
    }
}
