using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyGroupPlugin
{
    [TransactionAttribute(TransactionMode.Manual)] //Транзакция нужна при внесении изменений в модель,
                                                   //режим TransactionMode.Manual означает, что мы сами определяем в коде
                                                   //в какой момент она должна начаться и завершиться
    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements) //метод должен возвращать значение типа Result
        //которое указываетуспешно или нет завершилась команда, в случае, если команда завершена не успешно, все созданные транзакции откатываются ревитом
        //метод принимает 3 аргумента (при помощи первого: ExternalCommandData commandData - мы можем добраться к ревит, открытому документу,
        //базе данных открытого документа, при помощи второго: ref string message - изменяемое строковое сообщение,
        //суть его в том,что если возвращаемый результат будет Failed (неудача), то ревит отобразит сообщение об ошибке в окне,
        //третий аргумент: ElementSet elements - набор элементов, которые будут подсвечены в документе, если команда завершится неудачно
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument; //ссылка на активнй документ в ревит
                Document doc = uiDoc.Document; //через UIDocument можно получить ссылку на экземпляр класса Document, который будет содержать
                                               //базу данных элементов внутри открытого документа

                //создаем экземпляр созданного класса GroupPickFilter, чтобы передать его аргументов в метод PickObject
                GroupPickFilter groupPickFilter = new GroupPickFilter();
                
                    //на следующем этапе нужно попросить пользователя выбрать группу для копирования
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов"); //при помощи метода PickObject выбираем группу
                //получили ссылку на выбранную пользователем группу, но это не сам объект, если мы хотим скопировать или изменить объект, ссылки не достаточно,
                //нам нужен доступ к самому объекту - получить его можно при помощи метода GetElement, в качестве аргумента нужно указать либо Id элемента
                //либо ссылку reference
                Element element = doc.GetElement(reference);//как результат мы получаем объект типа элемент
                //чтобы можно было работать с объектов как  группой, нужно преобразовать element в group
                Group group = element as Group; //предпочтительнее преобразовывать через метод As, чтобы не было исключений

                
                XYZ groupCenter = GetElementCenter(group); //найдем центр группы припомощи созданного метода GetElementCenter

                //определяем комнату, в которой находится искомая группа объектов
                Room room = GetRoomByPoint(doc, groupCenter);

                //находим центр этой комнаты
                XYZ roomCenter = GetElementCenter(room);
                //находим смещение центра группы отностельно центра комнаты
                XYZ offset = groupCenter - roomCenter;


                //теперь нужно получить точку для вставки группы
                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");

                //пользователь выбирает точку где-то внутри комнаты
                //нужно воспользоваться методом GetRoomByPoint,чтобы определить комнату по которой щелкнул пользователь
                Room roomSelected = GetRoomByPoint(doc, point);
                //найти ее центр
                XYZ roomSelectedCenter = GetElementCenter(roomSelected);
                //на основе смещения вычислить точку в которую нужно вставить группу

                XYZ insertionPoint = roomSelectedCenter + offset;

                //далее нужно вставить группу в выбранную точку, для этого воспользоваться транзакцией
                Transaction transaction = new Transaction(doc);
                transaction.Start("Копирование группы объектов");
                //чтобы вставить группу в модель, нужно обратиться к документу, указать СВОЙСТВО "Create", дальше вызвать у него метод PlaceGroup,
                //в качестве аргумента передаем точку и тип группы
                doc.Create.PlaceGroup(insertionPoint, group.GroupType);

                transaction.Commit(); //метод commit подтверждает изменения
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException)//исключение, вызванное нажатием кнопки Esc
            {
                return Result.Cancelled; //вернуть результат - отмена
            }
            catch (Exception ex)
            {
                message = ex.Message; //чтобыпониматьиз-за чего произошло исключение, передадим сообщение об ошибке
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public XYZ GetElementCenter(Element element) //метод для нахождения центра элемента с использованием метода get_BoundingBox
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }


        public Room GetRoomByPoint(Document doc, XYZ point) //метод для определения комнаты по исходной точке
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms); //найдем все комнаты по категории в документе
            foreach(Element e in collector) //переберем комнаты
            {
                Room room = e as Room;
                if(room!=null)
                {
                    if (room.IsPointInRoom(point))
                        return room;
                }
            }
            return null;

                                                
        }
    }

    public class GroupPickFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                return true;
            else
                return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
