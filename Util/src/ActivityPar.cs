﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using hoTools.Utils;
using hoTools.Utils.Appls;
using hoTools.Utils.Parameter;

namespace hoTools.Utils.ActivityParameter
{
    //--------------------------------------------------------------------------------------------
    // Create / Update Activity Parameter from Operation
    //--------------------------------------------------------------------------------------------

    public class ActivityPar
    {
        //------------------------------------------------------------------------------
        // Create default Elements for Activity
        //------------------------------------------------------------------------------
        //
        // init
        // final
        static public bool activityIsSimple = true; // Create Activity from function in the simple form
        public static bool createDefaultElementsForActivity(EA.Repository rep, 
                                EA.Diagram dia, EA.Element act)
        {

            EA.DiagramObject initNode = null;
            
            // create init node
            initNode = CreateInitFinalNode(rep, dia, act, 100, "l=350;r=370;t=70;b=90;");
            act.Elements.Refresh();
            dia.DiagramObjects.Refresh();
            bool error = dia.Update();
            rep.ReloadDiagram(dia.DiagramID);

            return true;
        }

        // subtype=100 init node
        // subtype=101 final node

        public static EA.DiagramObject CreateInitFinalNode(EA.Repository rep, EA.Diagram dia, EA.Element act, 
                                      int subType, string position)
        {
            EA.Element initNode = (EA.Element)act.Elements.AddNew("", "StateNode");
            initNode.Subtype = subType;
            initNode.Update();
            if (dia != null)
            {
                Util.addSequenceNumber(rep, dia);
                EA.DiagramObject initDiaNode = (EA.DiagramObject)dia.DiagramObjects.AddNew(position,"");
                initDiaNode.ElementID = initNode.ElementID;
                initDiaNode.Sequence = 1;
                initDiaNode.Update();
                Util.setSequenceNumber(rep, dia, initDiaNode, "1");
                return initDiaNode;
            }else return null;
        }
        //--------------------------------------------------------------------------------
        // createActivityForOperation
        //--------------------------------------------------------------------------------
        // Create an Activity Diagram for the operation

        public static bool createActivityForOperation(EA.Repository rep, EA.Method m, int treePos=100)
        {
            // get class
            EA.Element elClass = rep.GetElementByID(m.ParentID);
            EA.Package pkgSrc = rep.GetPackageByID(elClass.PackageID);

            // create a package with the name of the operation
            EA.Package pkgTrg = (EA.Package)pkgSrc.Packages.AddNew(m.Name, "");
            pkgTrg.TreePos = treePos;
            pkgTrg.Update();
            pkgSrc.Packages.Refresh();

            EA.Element frm = null; // frame beneath package
            if (activityIsSimple == false)
            {
                // create Class Activity Diagram in target package
                EA.Diagram pkgActDia = (EA.Diagram)pkgTrg.Diagrams.AddNew("Operation:" + m.Name + " Content", "Activity");
                pkgActDia.Update();
                pkgTrg.Diagrams.Refresh();

                // add frame in Activity diagram
                EA.DiagramObject frmObj = (EA.DiagramObject)pkgActDia.DiagramObjects.AddNew("l=100;r=400;t=25;b=50", "");
                frm = (EA.Element)pkgTrg.Elements.AddNew(m.Name, "UMLDiagram");
                frm.Update();
                frmObj.ElementID = frm.ElementID;
                //frmObj.Style = "fontsz=200;pitch=34;DUID=265D32D5;font=Arial Narrow;bold=0;italic=0;ul=0;charset=0;";
                frmObj.Update();
                pkgTrg.Elements.Refresh();
                pkgActDia.DiagramObjects.Refresh();

            }
            // create activity with the name of the operation
            EA.Element act = (EA.Element)pkgTrg.Elements.AddNew(m.Name, "Activity");
            if (activityIsSimple == false)
            {
                act.Notes = "Generated from Operation:\r\n" + m.Visibility + " " + m.Name + ":" + m.ReturnType + ";\r\nDetails see Operation definition!!";
            }
            act.StereotypeEx = m.StereotypeEx;
            act.Update();
            pkgTrg.Elements.Refresh();

            // create activity diagram beneath Activity
            EA.Diagram actDia = (EA.Diagram)act.Diagrams.AddNew(m.Name, "Activity");
            // update diagram properties
            actDia.ShowDetails = 0; // hide details
            // scale page to din A4
            
            if (actDia.StyleEx.Length > 0)
            {
                actDia.StyleEx = actDia.StyleEx.Replace("HideQuals=0", "HideQuals=1"); // hide qualifier
            }
            else
            {
                actDia.StyleEx = "HideQuals=1;";
            }
            // Hide Qualifier
            if (actDia.ExtendedStyle.Length > 0)
            { actDia.ExtendedStyle = actDia.ExtendedStyle.Replace("ScalePI=0", "ScalePI=1"); ; }
            else 
            {  
                actDia.ExtendedStyle = "ScalePI=1;";
            }
            actDia.Update();
            act.Diagrams.Refresh();

            
            // put the activity on the diagram
            Util.addSequenceNumber(rep, actDia);
            EA.DiagramObject actObj = (EA.DiagramObject)actDia.DiagramObjects.AddNew("l=30;r=780;t=30;b=1120", "");
            actObj.ElementID = act.ElementID;
            actObj.Sequence = 1;
            actObj.Update();
            Util.setSequenceNumber(rep, actDia, actObj, "1");
            actDia.DiagramObjects.Refresh();

            // add default nodes (init/final)
            createDefaultElementsForActivity(rep, actDia, act);

            if (activityIsSimple == false)
            {
                // Add Heading to diagram
                Util.addSequenceNumber(rep, actDia);
                EA.DiagramObject noteObj = (EA.DiagramObject)actDia.DiagramObjects.AddNew("l=40;r=700;t=25;b=50", "");
                EA.Element note = (EA.Element)pkgTrg.Elements.AddNew("Text", "Text");

                note.Notes = m.Visibility + " " + elClass.Name + "_" + m.Name + ":" + m.ReturnType;
                note.Update();
                noteObj.ElementID = note.ElementID;
                noteObj.Style = "fontsz=200;pitch=34;DUID=265D32D5;font=Arial Narrow;bold=0;italic=0;ul=0;charset=0;";
                noteObj.Sequence = 1;
                noteObj.Update();
                Util.setSequenceNumber(rep, actDia, noteObj, "1");
            }
            pkgTrg.Elements.Refresh();
            actDia.DiagramObjects.Refresh();


            // Link Operation to activity
            Util.setBehaviorForOperation(rep, m, act);

            // Set show behavior
            Util.setShowBehaviorInDiagram(rep, m);

            // add parameters to activity
            updateParameterFromOperation(rep, act, m);
            int pos = 0;
            foreach (EA.Element actPar in act.EmbeddedElements)
            {
                if (! actPar.Type.Equals("ActivityParameter")) continue;
                Util.visualizePortForDiagramobject(pos, actDia, actObj, actPar, null);
                pos = pos + 1;
            }

            if (activityIsSimple == false)
            {
                // link Overview frame to diagram
                Util.setFrameLinksToDiagram(rep, frm, actDia);
                frm.Update();
            }

            // select operation
            rep.ShowInProjectView(m);
            return true;
        }
      

        public static EA.Diagram createActivityCompositeDiagram(EA.Repository rep, EA.Element act) {
            // create activity diagram beneath Activity
            EA.Diagram actDia = (EA.Diagram)act.Diagrams.AddNew(act.Name, "Activity");
            // update diagram properties
            actDia.ShowDetails = 0; // hide details
            // scale page to din A4
            
            if (actDia.StyleEx.Length > 0)
            {
                actDia.StyleEx = actDia.StyleEx.Replace("HideQuals=0", "HideQuals=1"); // hide qualifier
            }
            else
            {
                actDia.StyleEx = "HideQuals=1;";
            }
            // Hide Qualifier
            if (actDia.ExtendedStyle.Length > 0)
            { actDia.ExtendedStyle = actDia.ExtendedStyle.Replace("ScalePI=0", "ScalePI=1"); ; }
            else 
            {  
                actDia.ExtendedStyle = "ScalePI=1;";
            }
            actDia.Update();
            act.Diagrams.Refresh();

            // put the activity on the diagram
            Util.addSequenceNumber(rep, actDia);
            EA.DiagramObject actObj = (EA.DiagramObject)actDia.DiagramObjects.AddNew("l=30;r=780;t=30;b=1120", "");
            actObj.ElementID = act.ElementID;
            actObj.Update();
            actDia.DiagramObjects.Refresh();

            // add default nodes (init/final)
            createDefaultElementsForActivity(rep, actDia, act);
            act.Elements.Refresh();
            actDia.DiagramObjects.Refresh();
            return actDia;
        }

       
        //-------------------------------------------------------------------------------------------------
        // get Parameter from operation
        // visualize them on diagram / activity
        //-------------------------------------------------------------------------------------------------
        public static bool updateParameterFromOperation(EA.Repository rep, EA.Element act, EA.Method m)
        {
            if (m == null) return false;
            if (act.Locked) return false;
            if (!act.Type.Equals("Activity")) return false;

            EA.Element parTrgt = null;


            ///////////////////////////////////////////////////////////////////////////////////
            // return code
            string postfixName = "";
            string parName = "Return";
            int methodReturnTypID = 0;

            // is type defined?
            if ((m.ClassifierID != "0") & (m.ClassifierID != ""))
            {
                methodReturnTypID = Convert.ToInt32(m.ClassifierID);
            }
            
            // type is only defined as text
            else
            {
                methodReturnTypID = Convert.ToInt32(Util.getTypeID(rep, m.ReturnType));
            };

            bool withActivityReturnParameter = false;
            if (withActivityReturnParameter)
            {
                parTrgt.ClassifierID = methodReturnTypID;
                // create an return Parameter for Activity (in fact an element with properties)
                parTrgt = Util.getParameterFromActivity(rep, null, act, true);
                if (parTrgt == null)
                {
                    parTrgt = (EA.Element)act.EmbeddedElements.AddNew(parName, "Parameter");
                }
                else { parTrgt.Name = parName; }


                parTrgt.Alias = "return:" + m.ReturnType;
                parTrgt.ClassifierID = parTrgt.ClassifierID;

                parTrgt.Update();
                // update properties for returnvalue
                Param par = new Param(rep, parTrgt);
                par.setParameterProperties("direction", "out");
                par.save();
                par = null;
            }
            // returnType for activity
            act.ClassfierID = methodReturnTypID;
            act.Name = m.Name;

            // use stereotype of operation as stereotype for activity
            act.StereotypeEx = m.StereotypeEx;
            act.Update();
            act.EmbeddedElements.Refresh();

            // over all parameters
            string guids = "";
            foreach (EA.Parameter parSrc in m.Parameters)
            {
                // create an Parameter for Activity (in fact an element with properties)
                // - New if the parameter don't exists
                // - Update if the parameter exists
                // -- Update according to the parameter position

                //string direction = " [" + parSrc.Kind + "]";
                string direction = "";
                string prefixTyp = "";
                if (parSrc.IsConst) prefixTyp = " const";
                postfixName = "";
                if (parSrc.Kind.Contains("out")) postfixName = "*";
                parName = parSrc.Position.ToString() + ":" + parSrc.Name + postfixName + prefixTyp + direction;

                // check if parameter already exists (last parameter = false)
                parTrgt = Util.getParameterFromActivity(rep, parSrc, act);



                // parameter doesn't exists
                if (parTrgt == null)
                {
                    parTrgt = (EA.Element)act.EmbeddedElements.AddNew(parName, "Parameter");
                }
                else
                {
                    parTrgt.Name = parName;
                }
                guids = guids + parTrgt.ElementGUID;

                // is type defined?
                if ((parSrc.ClassifierID != "0") & (parSrc.ClassifierID != ""))
                {
                    parTrgt.ClassifierID = Convert.ToInt32(parSrc.ClassifierID);
                }
                // type is only defined as text
                else
                {   // try to find classifier
                    parTrgt.ClassifierID = Convert.ToInt32(Util.getTypeID(rep, parSrc.Type));
                    // use type in name (no classifier found)
                    if (parTrgt.ClassifierID == 0) parTrgt.Name = parTrgt.Name + ":" + parSrc.Type;
                };

                parTrgt.Notes = parSrc.Notes;
                parTrgt.Alias = "par_" + parSrc.Position.ToString() + ":" + parSrc.Type;

                // update properties for parameter
                Param par = new Param(rep, parTrgt);
                par.setParameterProperties("direction", parSrc.Kind);
                if (parSrc.IsConst)  par.setParameterProperties("constant", "true");
                par.save();
                par = null;
                parTrgt.Update();
               


            }
            act.EmbeddedElements.Refresh();
            // delete all unused parameter
            for (short i = (short)(act.EmbeddedElements.Count - 1); i >= 0; --i)
            {
                EA.Element embeddedEl = (EA.Element)act.EmbeddedElements.GetAt(i);
                if (embeddedEl.Type.Equals("ActivityParameter"))
                {
                    if (! (guids.Contains(embeddedEl.ElementGUID) )) {
                        act.EmbeddedElements.Delete(i);
                        }
                }
            }
            act.EmbeddedElements.Refresh();

            return true;
        }
        public static void visualizePortForDiagramobject(int pos, EA.Diagram dia, EA.DiagramObject diaObjSource, EA.Element port, EA.Element interf)
        {
            // check if port already exists
            foreach (EA.DiagramObject diaObj in dia.DiagramObjects)
            {
                if (diaObj.ElementID == port.ElementID) return;
            }

            // visualize ports
            int length = 6;
            // calculate target position
            int left = diaObjSource.right - length / 2;
            int right = left + length;
            int top = diaObjSource.top;
            int bottom = diaObjSource.bottom;

            top = top - 10 - pos * 10;
            bottom = top - length;
            string position = "l=" + left.ToString() + ";r=" + right.ToString() + ";t=" + top.ToString() + ";b=" + bottom.ToString() + ";";
            EA.DiagramObject diaObject = (EA.DiagramObject)dia.DiagramObjects.AddNew(position, "");
            dia.Update();
            if (port.Type.Equals("Port"))
            {
                // not showing label
                diaObject.Style = "LBL=CX=97:CY=13:OX=0:OY=0:HDN=1:BLD=0:ITA=0:UND=0:CLR=-1:ALN=0:ALT=0:ROT=0;";
            }
            else
            {

                // not showing label
                diaObject.Style = "LBL=CX=97:CY=13:OX=39:OY=3:HDN=0:BLD=0:ITA=0:UND=0:CLR=-1:ALN=0:ALT=0:ROT=0;";
            }
            diaObject.ElementID = port.ElementID;


            diaObject.Update();

            if (interf == null) return;

            // visualize interface
            EA.DiagramObject diaObject2 = (EA.DiagramObject)dia.DiagramObjects.AddNew(position, "");
            dia.Update();
            diaObject.Style = "LBL=CX=69:CY=13:OX=-69:OY=0:HDN=0:BLD=0:ITA=0:UND=0:CLR=-1:ALN=0:ALT=0:ROT=0;";
            diaObject2.ElementID = interf.ElementID;
            diaObject2.Update();

        }
    }
}
