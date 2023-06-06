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

                var roomPickFilter = new RoomPickFilter();
                var rooms = sel.PickObjects(ObjectType.Element, roomPickFilter, "Select target rooms for duplicate furniture group");

                var trans = new Transaction(doc);
                trans.Start("Lab");
                PlaceFurnitureInRooms(doc, rooms, sourceCenter, group.GroupType, origin);
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

        private XYZ GetElementCenter(Element element)
        {
            var boundingBox = element.get_BoundingBox(null);
            return (boundingBox.Max + boundingBox.Min) / 2;
        }

        private Room GetRoomOfGroup(Document doc, XYZ point)
        {
            var elements = new FilteredElementCollector(doc);
            elements.OfCategory(BuiltInCategory.OST_Rooms);
            Room room = null;
            foreach (var element in elements)
            {
                room = element as Room;
                if (room != null && room.IsPointInRoom(point))
                {
                    break;
                }
            }

            return room;
        }

        private XYZ GetRoomCenter(Room room)
        {
            var boundCenter = GetElementCenter(room);
            var locationPoint = (LocationPoint) room.Location;
            return new XYZ(boundCenter.X, boundCenter.Y, locationPoint.Point.Z);
        }

        private void PlaceFurnitureInRooms(Document doc,
                                          IList<Reference> rooms,
                                          XYZ sourceCenter,
                                          GroupType groupType,
                                          XYZ groupOrigin)
        {
            var offset = groupOrigin - sourceCenter;
            var offsetXY = new XYZ(offset.X, offset.Y, 0);
            foreach (var room in rooms)
            {
                var roomTarget = doc.GetElement(room) as Room;
                if (roomTarget != null)
                {
                    var roomCenter = GetRoomCenter(roomTarget);
                    doc.Create.PlaceGroup(roomCenter + offsetXY, groupType);
                }
            }
        }
    }

    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem.Category.Id.Value.Equals((long)BuiltInCategory.OST_IOSModelGroups);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }

    public class RoomPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return (elem.Category.Id.Value.Equals((long)BuiltInCategory.OST_Rooms));
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
