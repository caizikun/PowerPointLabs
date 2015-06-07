﻿using System.Collections.Generic;
using System.IO;
using Microsoft.Office.Interop.PowerPoint;

namespace FunctionalTestInterface
{
    public interface IPowerPointOperations
    {
        void EnterFunctionalTest();
        void ExitFunctionalTest();
        bool IsInFunctionalTest();

        Slide GetCurrentSlide();
        Slide SelectSlide(int index);
        Slide SelectSlide(string slideName);

        Selection GetCurrentSelection();
        ShapeRange SelectShapes(string shapeName);
        ShapeRange SelectShapes(List<string> shapeNames);
        ShapeRange SelectShapesByPrefix(string prefix);
        FileInfo ExportSelectedShapes();
    }
}
