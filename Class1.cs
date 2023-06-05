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
            try
            {

                UIApplication uiapp = commandData.Application;
                Document doc = uiapp.ActiveUIDocument.Document;

                Selection sel = uiapp.ActiveUIDocument.Selection;

                GroupPickFilter selectionFilter = new GroupPickFilter();
                Reference pickedReference = sel.PickObject(ObjectType.Element, selectionFilter, "Please select a group");
                Element elem = doc.GetElement(pickedReference);
                Group group = elem as Group;

                XYZ point = sel.PickPoint("Pick a point to place group");

                Transaction trans = new Transaction(doc);
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
    }

    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem.Category.Id.Value.Equals((int)BuiltInCategory.OST_IOSModelGroups);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
