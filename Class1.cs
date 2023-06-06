using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;

namespace PlaceGroup
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Class1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,
                              ref string message,
                              ElementSet elements)
        {
            var uiapp = commandData.Application;
            var doc = uiapp.ActiveUIDocument.Document;
            var sel = uiapp.ActiveUIDocument.Selection;
            var selectionFilter = new GroupPickFilter();

            try
            {
                var pickedReference = sel.PickObject(ObjectType.Element, selectionFilter, "Please select a group");
                var elem = doc.GetElement(pickedReference);
                var group = elem as Group;

                var origin = GetElementCenter(group);
                var room = GetRoomOfGroup(doc, origin);
                var sourceCenter = GetRoomCenter(room);
                var coords = $"X: {sourceCenter.X} \r\n Y: {sourceCenter.Y} \r\n Z: {sourceCenter.Z}";
                TaskDialog.Show("Source room Center", coords);

                //var point = sel.PickPoint("Pick a point to place group");
                var point = sourceCenter + new XYZ(20, 0, 0);

                var trans = new Transaction(doc);
                trans.Start("Lab");
                doc.Create.PlaceGroup(point, group.GroupType);
                trans.Commit();

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        public XYZ GetElementCenter(Element element)
        {
            var boundingBox = element.get_BoundingBox(null);
            return (boundingBox.Max + boundingBox.Min) / 2;
        }

        Room GetRoomOfGroup(Document doc, XYZ point)
        {
            var collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            Room room = null;
            foreach (var element in collector)
            {
                room = element as Room;
                if (room != null && room.IsPointInRoom(point))
                {
                    break;
                }
            }

            return room;
        }

        public XYZ GetRoomCenter(Room room)
        {
            var boundCenter = GetElementCenter(room);
            var locationPoint = (LocationPoint) room.Location;
            return new XYZ(boundCenter.X, boundCenter.Y, locationPoint.Point.Z);
        }
    }

    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem.Category.Id.IntegerValue.Equals((int)BuiltInCategory.OST_IOSModelGroups);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
