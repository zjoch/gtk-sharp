// GtkSharp.Generation.StructBase.cs - The Structure/Object Base Class.
//
// Author: Mike Kestner <mkestner@speakeasy.net>
//
// (c) 2001 Mike Kestner

namespace GtkSharp.Generation {

	using System;
	using System.Collections;
	using System.IO;
	using System.Text.RegularExpressions;
	using System.Xml;

	public class StructBase : ClassBase {
	
		ArrayList fields = new ArrayList ();

		public StructBase (XmlElement ns, XmlElement elem) : base (ns, elem)
		{
			foreach (XmlNode node in elem.ChildNodes) {

				if (!(node is XmlElement)) continue;
				XmlElement member = (XmlElement) node;

				switch (node.Name) {
				case "field":
					fields.Add (member);
					break;

				case "callback":
					Statistics.IgnoreCount++;
					break;

				default:
					if (!IsNodeNameHandled (node.Name))
						Console.WriteLine ("Unexpected node " + node.Name + " in " + CName);
					break;
				}
			}
		}

		public override string MarshalType {
			get
			{
				return "ref " + QualifiedName;
			}
		}

		public override string MarshalReturnType {
			get
			{
				return "IntPtr";
			}
		}

		public override string CallByName (string var_name)
		{
			return "ref " + var_name;
		}

		public override string CallByName ()
		{
			return "ref this";
		}

		public override string AssignToName {
			get { return "raw"; }
		}

		public override string FromNative(string var)
		{
			return var;
		}
		
		public override string FromNativeReturn(string var)
		{
			return QualifiedName + ".New (" + var + ")";
		}

		public override string ToNativeReturn(string var)
		{
			// FIXME
			return var;
		}

		private bool DisableNew {
			get {
				return Elem.HasAttribute ("disable_new");
			}
		}

		protected void GenFields (StreamWriter sw)
		{
			Field.bitfields = 0;
			bool need_field = true;
			foreach (XmlElement field_elem in fields) {
				Field field = new Field (field_elem);
				if (field.IsBit) {
					if (need_field)
						need_field = false;
					else
						continue;
				} else
					need_field = true;
				field.Generate (sw);	
			}
		}

		public virtual void Generate (GenerationInfo gen_info)
		{
			bool need_close = false;
			if (gen_info.Writer == null) {
				gen_info.Writer = gen_info.OpenStream (Name);
				need_close = true;
			}

			StreamWriter sw = gen_info.Writer;
			
			sw.WriteLine ("namespace " + NS + " {");
			sw.WriteLine ();
			sw.WriteLine ("\tusing System;");
			sw.WriteLine ("\tusing System.Collections;");
			sw.WriteLine ("\tusing System.Runtime.InteropServices;");
			sw.WriteLine ();
			
			sw.WriteLine ("#region Autogenerated code");
			sw.WriteLine ("\t[StructLayout(LayoutKind.Sequential)]");
			sw.WriteLine ("\tpublic struct " + Name + " {");
			sw.WriteLine ();

			GenFields (sw);
			sw.WriteLine ();
			GenCtors (gen_info);
			GenMethods (gen_info, null, null);

			if (!need_close)
				return;

			sw.WriteLine ("#endregion");
			AppendCustom(sw, gen_info.CustomDir);
			
			sw.WriteLine ("\t}");
			sw.WriteLine ("}");
			sw.Close ();
			gen_info.Writer = null;
		}
		
		protected override void GenCtors (GenerationInfo gen_info)
		{
			StreamWriter sw = gen_info.Writer;

			sw.WriteLine ("\t\tpublic static {0} Zero = new {0} ();", QualifiedName);
			sw.WriteLine();
			if (!DisableNew) {
				sw.WriteLine ("\t\tpublic static " + QualifiedName + " New(IntPtr raw) {");
				sw.WriteLine ("\t\t\tif (raw == IntPtr.Zero) {");
				sw.WriteLine ("\t\t\t\treturn {0}.Zero;", QualifiedName);
				sw.WriteLine ("\t\t\t}");
				sw.WriteLine ("\t\t\t{0} self = new {0}();", QualifiedName);
				sw.WriteLine ("\t\t\tself = ({0}) Marshal.PtrToStructure (raw, self.GetType ());", QualifiedName);
				sw.WriteLine ("\t\t\treturn self;");
				sw.WriteLine ("\t\t}");
				sw.WriteLine ();
			}

			foreach (Ctor ctor in Ctors) {
				ctor.ForceStatic = true;
				if (ctor.Params != null)
					ctor.Params.Static = true;
			}

			base.GenCtors (gen_info);
		}

	}
}

