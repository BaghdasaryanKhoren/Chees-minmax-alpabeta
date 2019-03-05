using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SrcChess2 {
    /// <summary>
    /// Do the lexical analysis of a PGN document
    /// </summary>
    public class PgnLexical {

        /// <summary>
        /// Token type
        /// </summary>
        public enum TokenTypeE {
            /// <summary>Integer value</summary>
            TOK_Integer,
            /// <summary>String value</summary>
            TOK_String,
            /// <summary>Symbol</summary>
            TOK_Symbol,
            /// <summary>Single DOT</summary>
            TOK_Dot,
            /// <summary>NAG value</summary>
            TOK_NAG,
            /// <summary>Openning square bracket</summary>
            TOK_OpenSBracket,
            /// <summary>Closing square bracket</summary>
            TOK_CloseSBracket,
            /// <summary>Termination symbol</summary>
            TOK_Termination,
            /// <summary>Unknown token</summary>
            TOK_UnknownToken,
            /// <summary>Comment</summary>
            TOK_Comment,
            /// <summary>End of file</summary>
            TOK_EOF
        }

        /// <summary>
        /// Token value
        /// </summary>
        public struct Token {
            /// <summary>Token type</summary>
            public  TokenTypeE  eType;
            /// <summary>Token string value if any</summary>
            public  string      strValue;
            /// <summary>Token integer value if any</summary>
            public  int         iValue;
            /// <summary>Token starting position</summary>
            public  long        lStartPos;
            /// <summary>Token size</summary>
            public  int         iSize;
        }

        private const int       MaxBufferSize = 1048576;
        /// <summary>List of buffers</summary>
        private List<Char[]>    m_listBuffer;
        /// <summary>Position in the buffer</summary>
        private int             m_iPosInBuffer;
        /// <summary>Position in the list</summary>
        private int             m_iPosInList;
        /// <summary>Current array</summary>
        private char[]          m_curArray;
        /// <summary>Current array size</summary>
        private int             m_iCurArraySize;
        /// <summary>Position within the raw array</summary>
        private long            m_lCurBasePos;
        /// <summary>Text size</summary>
        private long            m_lTextSize;
        /// <summary>Pushed character if any</summary>
        private Char?           m_chrPushed;
        /// <summary>true if at the first character of a line</summary>
        private bool            m_bFirstChrInLine;
        /// <summary>Pushed token</summary>
        private Token?          m_tokPushed;

        /// <summary>
        /// Ctor
        /// </summary>
        public PgnLexical() {
            Clear(true /*bAllocateEmpty*/);
        }

        /// <summary>
        /// Clear all buffers
        /// </summary>
        /// <param name="bAllocateEmpty">   true to allocate an empty block</param>
        public void Clear(bool bAllocateEmpty) {
            m_listBuffer        = new List<char[]>(256);
            m_iPosInBuffer      = 0;
            m_iPosInList        = 0;
            m_lCurBasePos       = 0;
            m_lTextSize         = 0;
            m_chrPushed         = null;
            m_tokPushed         = null;
            m_bFirstChrInLine   = true;
            if (bAllocateEmpty) {
                m_listBuffer.Add(new char[0]);
            }
        }

        /// <summary>
        /// Current position
        /// </summary>
        public long CurrentPosition {
            get {
                return(m_lCurBasePos + m_iPosInBuffer);
            }
        }

        /// <summary>
        /// Text size
        /// </summary>
        public long TextSize {
            get {
                return(m_lTextSize);
            }
        }

        /// <summary>
        /// Gets the number of buffer which has been allocated
        /// </summary>
        public int BufferCount {
            get {
                return(m_listBuffer.Count);
            }

        }

        /// <summary>
        /// Current buffer position
        /// </summary>
        public int CurrentBufferPos {
            get {
                return(m_iPosInList);
            }
        }

        /// <summary>
        /// Initialize the buffer from a file
        /// </summary>
        /// <param name="strInpFileName">   File name to open</param>
        /// <returns>
        /// Stream or null if unable to open the file.
        /// </returns>
        public bool InitFromFile(string strInpFileName) {
            bool            bRetVal         = false;
            FileStream      streamInp       = null;
            StreamReader    streamReader    = null;

            try {
                streamInp = File.OpenRead(strInpFileName);
                if (streamInp != null) {
                    streamReader = new StreamReader(streamInp, Encoding.GetEncoding(1252), true, 65536);
                    streamInp    = null;
                    ReadInMemory(streamReader);
                    bRetVal = true;
                }
            } catch(System.Exception ex) {
                if (streamInp != null) {
                    streamInp.Dispose();
                } else if (streamReader != null) {
                    streamReader.Dispose();
                }
                System.Windows.MessageBox.Show("Unable to read the file - " + strInpFileName + ".\r\n" + ex.Message);
            }
            return(bRetVal);
        }

        /// <summary>
        /// Initialize from string
        /// </summary>
        /// <param name="strText">  Text string</param>
        public void InitFromString(string strText) {
            Clear(false  /*bAllocateEmpty*/);
            m_listBuffer.Add(strText.ToArray());
            m_curArray      = m_listBuffer[0];
            m_iCurArraySize = m_curArray.Length;
            m_lTextSize     = m_iCurArraySize;
        }

        /// <summary>
        /// Fill the buffer
        /// </summary>
        private void ReadInMemory(StreamReader streamReader) {
            char[]  arr;
            char[]  arrTmp;
            int     iReadSize;

            Clear(false  /*bAllocateEmpty*/);
            arr             = new char[MaxBufferSize];
            iReadSize       = streamReader.ReadBlock(arr, 0, MaxBufferSize);
            m_lTextSize     = 0;
            while (iReadSize == MaxBufferSize) {
                m_lTextSize += MaxBufferSize;
                m_listBuffer.Add(arr);
                arr         = new char[MaxBufferSize];
                iReadSize   = streamReader.ReadBlock(arr, 0, MaxBufferSize);
            }
            if (iReadSize != 0) {
                m_lTextSize += iReadSize;
                arrTmp       = new char[iReadSize];
                for (int i = 0; i < iReadSize; i++) {
                    arrTmp[i] = arr[i];
                }
                m_listBuffer.Add(arrTmp);
            }
            if (m_listBuffer.Count == 0) {
                m_listBuffer.Add(new char[0]);
            }
            m_curArray      = m_listBuffer[0];
            m_iCurArraySize = m_curArray.Length;
        }

        /// <summary>
        /// Select the next buffer in list
        /// </summary>
        /// <returns>
        /// true if succeed, false if EOF
        /// </returns>
        private bool SelectNextBuffer() {
            bool    bRetVal;

            if (m_iPosInList + 1 < m_listBuffer.Count) {
                m_lCurBasePos  += m_curArray.Length;
                m_curArray      = m_listBuffer[++m_iPosInList];
                m_iPosInBuffer  = 0;
                m_iCurArraySize = m_curArray.Length;
                bRetVal         = true;
            } else {
                bRetVal         = false;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Peek a character
        /// </summary>
        /// <returns>
        /// Character or 0 if EOF
        /// </returns>
        public Char PeekChr() {
            Char    cRetVal;
            Char[]  arr;

            if (m_chrPushed.HasValue) {
                cRetVal = m_chrPushed.Value;
            } else if (m_iPosInBuffer < m_iCurArraySize) {
                cRetVal = m_curArray[m_iPosInBuffer];
            } else if (m_iPosInList + 1 < m_listBuffer.Count) {
                arr     = m_listBuffer[m_iPosInList + 1];
                cRetVal = (arr.Length == 0) ? '\0' : arr[0];
            } else {
                cRetVal = '\0';
            }
            return(cRetVal);
        }

        /// <summary>
        /// Get the next character
        /// </summary>
        /// <returns>
        /// Character or 0 if EOF
        /// </returns>
        private Char GetChrInt() {
            Char    cRetVal;

            if (m_chrPushed.HasValue) {
                cRetVal        = m_chrPushed.Value;
                m_chrPushed    = null;
            } else if (m_iPosInBuffer < m_iCurArraySize) {
                cRetVal         = m_curArray[m_iPosInBuffer++];
            } else if (SelectNextBuffer()) {
                if (m_iCurArraySize > 0) {
                    m_iPosInBuffer  = 1;
                    cRetVal         = m_curArray[0];
                } else {
                    m_iPosInBuffer  = 0;
                    cRetVal         = '\0';
                }
            } else {
                cRetVal = '\0';
            }
            if (cRetVal == '\r') {
                m_bFirstChrInLine = true;
            } else if (cRetVal != '\n') {
                m_bFirstChrInLine   = false;
            }
            return(cRetVal);
        }

        /// <summary>
        /// Push back a character
        /// </summary>
        /// <param name="chr">  Character to push</param>
        public void PushChr(Char chr) {
            if (m_chrPushed == null) {
                m_chrPushed = chr;
            } else {
                throw new MethodAccessException("Cannot push two characters!");
            }
        }

        /// <summary>
        /// Skip whitespace
        /// </summary>
        public void SkipSpace() {
            Char    chr;
            bool    bNextArray;

            if (!m_chrPushed.HasValue || (chr = m_chrPushed.Value) == ' ' || chr == '\r' || chr == '\n' || chr == (char)9) {
                m_chrPushed = null;
                do {
                    while (m_iPosInBuffer < m_iCurArraySize && ((chr = m_curArray[m_iPosInBuffer]) == ' ' || chr == '\r' || chr == '\n' || chr == (char)9)) {
                        if (chr == '\r') {
                            m_bFirstChrInLine = true;
                        } else if (chr != '\n') {
                            m_bFirstChrInLine = false;
                        }
                        m_iPosInBuffer++;
                    }
                    if (m_iPosInBuffer < m_iCurArraySize) {
                        bNextArray  = false;
                    } else {
                        bNextArray  = SelectNextBuffer();
                    }
                } while (bNextArray);
            }
        }

        /// <summary>
        /// Skip the rest of the line
        /// </summary>
        private void SkipLine() {
            char    chr;

            do {
                chr = GetChrInt();
            } while (chr != '\r' && chr != '\0');
            while (PeekChr() == '\n') {
                GetChrInt();
            }
            m_bFirstChrInLine = true;
        }

        /// <summary>
        /// Get a character
        /// </summary>
        /// <returns>
        /// Character
        /// </returns>
        public Char GetChr() {
            Char    cRetVal;
            bool    bContinue;

            do {
                cRetVal     = GetChrInt();
                bContinue   = (m_bFirstChrInLine && (cRetVal == ';' || cRetVal == '%'));
                if (bContinue) {
                    SkipLine();
                }
            } while (bContinue);
            return(cRetVal);
        }

        /// <summary>
        /// Gets the string at the specified position
        /// </summary>
        /// <param name="lStartingPos"> Starting position in text</param>
        /// <param name="iLength">      String size</param>
        /// <returns>
        /// String or null if bad position specified
        /// </returns>
        public string GetStringAtPos(long lStartingPos, int iLength) {
            string          strRetVal;
            int             iPosInBuf;
            int             iPosInList;
            int             iMaxSize;
            char[]          arr = null;
            StringBuilder   strb;

            if (iLength > MaxBufferSize) {
                throw new ArgumentException("Length too big");
            } else if (iLength == 0) {
                strRetVal = "<empty>";
            } else {
                strb        = new StringBuilder(iLength + 1);
                iPosInList  = (int)(lStartingPos / MaxBufferSize);
                iPosInBuf   = (int)(lStartingPos % MaxBufferSize);
                if (iPosInList < m_listBuffer.Count) {
                    arr         = m_listBuffer[iPosInList];
                    iMaxSize    = arr.Length - iPosInBuf;
                    if (iLength <= iMaxSize) {
                        strb.Append(arr, iPosInBuf, iLength);
                    } else if (iPosInList < m_listBuffer.Count) {
                        strb.Append(arr, iPosInBuf, iMaxSize);
                        strb.Append(m_listBuffer[iPosInList+1], 0, iLength - iMaxSize);
                    } else {
                        arr     = null;
                    }
                }
                strRetVal = (iPosInList == -1) ? null : strb.ToString();
            }
            return(strRetVal);
        }

        /// <summary>
        /// Returns if the text is probably a single FEN (no more than one line)
        /// </summary>
        /// <returns>
        /// true if probably a single FEN
        /// </returns>
        public bool IsOnlyFEN() {
            bool    bRetVal;

            if (m_listBuffer.Count > 1) {
                bRetVal = false;
            } else {
                bRetVal = (m_listBuffer[0].Count(x => x == '\r')) <= 1;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Flush old buffer to save memory
        /// </summary>
        public void FlushOldBuffer() {
            int     iIndex;

            iIndex  = m_iPosInList - 2;
            while (iIndex >= 0 && m_listBuffer[iIndex] != null) {
                m_listBuffer[iIndex] = null;
                iIndex--;
            }
        }


        /// <summary>
        /// Fetch a string token
        /// </summary>
        /// <returns>
        /// String
        /// </returns>
        private string GetStringToken() {
            Char            cChr;
            StringBuilder   strb;

            strb = new StringBuilder();
            do {
                cChr = GetChr();
                switch(cChr) {
                case '\r':
                    throw new PgnParserException("String cannot return a new line");
                case '\0':
                    throw new PgnParserException("Missing string termination quote");
                case '\\':
                    cChr = GetChr();
                    if (cChr == '"') {
                        strb.Append(cChr);
                    } else {
                        strb.Append('\\');
                        strb.Append(cChr);
                    }
                    break;
                case '"':
                    break;
                default:
                    strb.Append(cChr);
                    break;
                }
            } while (cChr != '"');
            return(strb.ToString());
        }

        /// <summary>
        /// Get an integer
        /// </summary>
        /// <returns>
        /// Integer value
        /// </returns>
        private int GetIntegerToken(Char cFirstChr) {
            int     iRetVal;
            Char    chr;

            iRetVal = (cFirstChr - '0');
            while ((chr = GetChr()) >= '0' && chr <= '9') {
                iRetVal = iRetVal * 10 + (chr - '0');
            }
            PushChr(chr);
            return(iRetVal);
        }

        /// <summary>
        /// Fetch a symbol token
        /// </summary>
        /// <param name="chrFirst">     First character</param>
        /// <param name="bAllDigit">    true if symbol is only composed of digit</param>
        /// <param name="bFoundSlash">  Found a slash in the symbol. Only valid for 1/2-1/2</param>
        /// <returns>
        /// Symbol
        /// </returns>
        private string GetSymbolToken(Char chrFirst, out bool bAllDigit, out bool bFoundSlash) {
            Char            chr;
            StringBuilder   strb;

            bFoundSlash = false;
            bAllDigit   = (chrFirst >= '0' && chrFirst <= '9');
            strb        = new StringBuilder();
            strb.Append(chrFirst);
            chr         = GetChr();
            while ((chr >= 'a' && chr <= 'z')   || 
                   (chr >= 'A' && chr <= 'Z')   ||
                   (chr >= '0' && chr <= '9')   ||
                   (chr == '_')                 ||
                   (chr == '+')                 ||
                   (chr == '#')                 ||
                   (chr == '=')                 ||
                   (chr == ':')                 ||
                   (chr == '-')                 ||
                   (chr == '/')) {
                if (chr == '/') {
                    bFoundSlash = true;
                }
                strb.Append(chr);
                if (bAllDigit && (chr < '0' || chr > '9')) {
                    bAllDigit = false;
                }
                chr = GetChr();
            }
            PushChr(chr);
            return(strb.ToString());
        }

        /// <summary>
        /// Get the next token
        /// </summary>
        /// <returns>
        /// Token
        /// </returns>
        public Token GetNextToken() {
            Token   tokRetVal;
            Char    chr;
            bool    bAllDigit;
            bool    bFoundSlash;
            bool    bComment;
            int     iParCount;

            if (m_tokPushed.HasValue) {
                tokRetVal   = m_tokPushed.Value;
                m_tokPushed = null;
            } else {
                tokRetVal   = new Token();
                do {
                    SkipSpace();
                    bComment            = false;
                    tokRetVal.lStartPos = CurrentPosition;
                    chr                 = GetChr();
                    switch(chr) {
                    case '\0':
                        tokRetVal.eType     = TokenTypeE.TOK_EOF;
                        break;
                    case '\"':
                        tokRetVal.eType     = TokenTypeE.TOK_String;
                        tokRetVal.strValue  = GetStringToken();
                        tokRetVal.iSize     = (int)(CurrentPosition - tokRetVal.lStartPos);
                        break;
                    case '.':
                        tokRetVal.eType     = TokenTypeE.TOK_Dot;
                        while (PeekChr() == '.') {
                            GetChr();
                        }
                        tokRetVal.iSize     = (int)(CurrentPosition - tokRetVal.lStartPos + 1);
                        break;
                    case '$':
                        chr = GetChr();
                        if (chr < '0' || chr > '9') {
                            throw new PgnParserException("Invalid NAG");
                        } else {
                            tokRetVal.eType     = TokenTypeE.TOK_NAG;
                            tokRetVal.iValue    = GetIntegerToken(chr);
                        }
                        tokRetVal.iSize     = (int)(CurrentPosition - tokRetVal.lStartPos - 1);
                        break;
                    case '[':
                        tokRetVal.eType     = TokenTypeE.TOK_OpenSBracket;
                        tokRetVal.iSize     = 1;
                        break;
                    case ']':
                        tokRetVal.eType     = TokenTypeE.TOK_CloseSBracket;
                        tokRetVal.iSize     = 1;
                        break;
                    case '{':
                        bComment    = true;
                        while ((chr = GetChr()) != 0 && chr != '}');
                        break;
                    case '(':
                        bComment    = true;
                        iParCount   = 1;
                        while (iParCount != 0 && (chr = GetChr()) != 0) {
                            if (chr == '(') {
                                iParCount++;
                            } else if (chr == ')') {
                                iParCount--;
                            } else if (chr == '{') {
                                while ((chr = GetChr()) != 0 && chr != '}');
                            }
                        }
                        break;
                    case '-':
                        tokRetVal.eType     = TokenTypeE.TOK_UnknownToken;
                        tokRetVal.strValue  = GetSymbolToken('-', out bAllDigit, out bFoundSlash);
                        break;
                    case '*':
                        tokRetVal.eType     = TokenTypeE.TOK_Termination;
                        tokRetVal.strValue  = "*";
                        tokRetVal.iSize     = 1;
                        break;
                    default:
                        if ((chr >= 'a' && chr <= 'z') || 
                            (chr >= 'A' && chr <= 'Z') ||
                            (chr >= '0' && chr <= '9')) {
                            tokRetVal.strValue  = GetSymbolToken(chr, out bAllDigit, out bFoundSlash);
                            tokRetVal.iSize     = (int)(CurrentPosition - tokRetVal.lStartPos - 1);
                            if (bAllDigit) {
                                tokRetVal.eType  = TokenTypeE.TOK_Integer;
                                tokRetVal.iValue = Int32.Parse(tokRetVal.strValue);
                            } else {
                                switch(tokRetVal.strValue) {
                                case "0-1":
                                case "1-0":
                                case "1/2-1/2":
                                    tokRetVal.eType = TokenTypeE.TOK_Termination;
                                    break;
                                default:
                                    if (bFoundSlash) {
                                        throw new PgnParserException("'/' character found at an unexpected location.");
                                    }
                                    tokRetVal.eType = TokenTypeE.TOK_Symbol;
                                    break;
                                }
                            }
                        } else {
                            throw new PgnParserException("Unknown token character '" + chr + "'");
                        }
                        break;
                    }
                } while (bComment);
            }
            return(tokRetVal);
        }

        /// <summary>
        /// Assume the specified token
        /// </summary>
        /// <param name="eType">    Token type</param>
        /// <param name="tok">      Assumed token</param>
        /// <returns>
        /// Token
        /// </returns>
        public void AssumeToken(TokenTypeE eType, Token tok) {
            if (tok.eType != eType) {
                throw new PgnParserException("Expecing a token of type - " + eType.ToString(),
                                             GetStringAtPos(tok.lStartPos, tok.iSize));
            }
        }

        /// <summary>
        /// Assume the specified token
        /// </summary>
        /// <param name="eType">    Token type</param>
        /// <returns>
        /// Token
        /// </returns>
        public Token AssumeToken(TokenTypeE eType) {
            Token   tokRetVal;

            tokRetVal = GetNextToken();
            AssumeToken(eType, tokRetVal);
            return(tokRetVal);
        }

        /// <summary>
        /// Push back a token
        /// </summary>
        /// <returns>
        /// Token
        /// </returns>
        public void PushToken(Token tok) {
            if (!m_tokPushed.HasValue) {
                m_tokPushed = tok;
            } else {
                throw new MethodAccessException("Cannot push two tokens!");
            }
        }

        /// <summary>
        /// Peek a token
        /// </summary>
        /// <returns>
        /// Token
        /// </returns>
        public Token PeekToken() {
            Token   tokRetVal;

            tokRetVal = GetNextToken();
            PushToken(tokRetVal);
            return(tokRetVal);
        }
    }
}
