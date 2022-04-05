using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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
            UIDocument uiDoc = commandData.Application.ActiveUIDocument; //ссылка на активнй документ в ревит
            Document doc = uiDoc.Document; //через UIDocument можно получить ссылку на экземпляр класса Document, который будет содержать
            //базу данных элементов внутри открытого документа

            //на следующем этапе нужно попросить пользователя выбрать группу для копирования
            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "Выберите группу объектов"); //при помощи метода PickObject выбираем группу
            //получили ссылку на выбранную пользователем группу, но это не сам объект, если мы хотим скопировать или изменить объект, ссылки не достаточно,
            //нам нужен доступ к самому объекту - получить его можно при помощи метода GetElement, в качестве аргумента нужно указать либо Id элемента
            //либо ссылку reference
            Element element = doc.GetElement(reference);//как результат мы получаем объект типа элемент
            //чтобы можно было работать с объектов как  группой, нужно преобразовать element в group
            Group group = element as Group; //предпочтительнее преобразовывать через метод As, чтобы не было исключений

            //теперь нужно получить точку для вставки группы
            XYZ point = uiDoc.Selection.PickPoint("Выберите точку");

            //далее нужно вставить группу в выбранную точку, для этого воспользоваться транзакцией
            Transaction transaction = new Transaction(doc);
            transaction.Start("Копирование группы объектов");
            //чтобы вставить группу в модель, нужно обратиться к документу, указать СВОЙСТВО "Create", дальше вызвать у него метод PlaceGroup,
            //в качестве аргумента передаем точку и тип группы
            doc.Create.PlaceGroup(point, group.GroupType);
            
            transaction.Commit(); //метод commit подтверждает изменения

            return Result.Succeeded;







        }
    }
}
 