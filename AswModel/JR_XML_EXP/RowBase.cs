// Decompiled with JetBrains decompiler
// Type: JR_XML_EXP.RowBase
// Assembly: JR_XML_EXP, Version=1.0.0.11, Culture=neutral, PublicKeyToken=null
// MVID: 7E5E55B6-4EBA-4169-9857-577456847AA3
// Assembly location: \\ropid\dfs\JRKLIENT\JR_XML_EXP.exe

using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace JR_XML_EXP
{
  [Serializable]
  public class RowBase
  {
    public static void Serialize(object oObject, bool bIndent, string SouborXML)
    {
      XmlSerializer xmlSerializer = new XmlSerializer(oObject.GetType());
      XmlWriterSettings settings = new XmlWriterSettings()
      {
        Indent = bIndent,
        OmitXmlDeclaration = false,
        Encoding = Encoding.UTF8
      };
      XmlWriter xmlWriter1 = XmlWriter.Create(SouborXML, settings);
      XmlWriter xmlWriter2 = xmlWriter1;
      object objectValue = RuntimeHelpers.GetObjectValue(oObject);
      xmlSerializer.Serialize(xmlWriter2, objectValue);
      xmlWriter1.Flush();
      xmlWriter1.Close();
    }
  }
}
