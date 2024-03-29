/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System;
using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Generation;

namespace Microsoft.Scripting.Ast {
    public class DebugStatement : Statement {
        private readonly string /*!*/ _marker;

        internal DebugStatement(string /*!*/ marker)
            : base(AstNodeType.DebugStatement, SourceSpan.None) {
            _marker = marker;
        }

        public string Marker {
            get { return _marker; }
        }

        public override void Emit(CodeGen cg) {
            cg.EmitDebugMarker(_marker);
        }
    }

    public static partial class Ast {
        public static DebugStatement DebugMarker(string marker) {
            Contract.RequiresNotNull(marker, "marker");
            return new DebugStatement(marker);
        }

        public static Expression DebugMark(Expression expression, string marker) {
            return Comma(Ast.Void(DebugMarker(marker)), expression);
        }
    }
}
