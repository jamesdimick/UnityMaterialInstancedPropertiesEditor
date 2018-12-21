# Unity Material Instanced Properties Editor
A generic editor for manipulating instanced properties of materials in Unity.

![Screenshot of the editor in Unity](Screenshot.png?raw=true)

# Unity Version
It was created in Unity **2018.3.0** but it should work fine in older versions _(unsure how far back)_. Please submit a report if you find any issues.

# Limitations
* There is currently no way to tell between an instanced material property and a non-instanced material property on the C# side in Unity. This means that **all** properties are always included in this component. You must keep track yourself which properties are instanced in your shaders and only change those specific properties via this component. Changing non-instanced properties via this component yields undefined behaviour and can even cause performance issues if Unity decides to create new material instances behind the scenes.

* This component also displays normally-hidden properties. Use caution when modifying these properties.

* Some more advanced properties cannot be properly represented via this component due to limitations with accessing property info in the currently available Unity APIs.
