### **QAttribute** 
![Nuget](https://img.shields.io/nuget/v/QAttribute)
![Nuget](https://img.shields.io/nuget/dt/QAttribute)
 ---
#### **[QBinding]**
By adding the [QBinding] attribute to the class, it automatically creates a binding property by adding only a field.

#### **[QNoBinding]**
If you do not want the property to be automatically bindable through the field, add the [QNoBinding] attribute to the field.

#### **[QVirtual]**
If you want to make a virtual property that can be automatically bound through the field, add the [QVirtual] attribute to the field.

#### **[QEvent]**
To add a PropertyChanging or PropertyChanged event, add [QEvent (PropertyChanging = "ChangingEvent()", PropertyChanged = "ChangedEvent()")] to the field

 ---
 
##### **Additional Resources**
##### https://andrewlock.net/series/creating-a-source-generator/
