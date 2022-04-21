using System;
namespace CPU6502
{
    public class CPU
    {

        // Memory


        Memory memory = new Memory();

        // Registers and flags
        UInt16 PC = 0x1c22;
        byte SP = 0xfe;
        byte A = 0;
        byte X = 0;
        byte Y = 0;
        bool CARRY_FLAG = false;
        bool ZERO_FLAG = false;
        bool OVERFLOW_FLAG = false;
        bool INTERRUPT_DISABLE = false;
        bool DECIMAL_MODE = false;
        bool NEGATIVE_FLAG = false;
        bool BREAK_FLAG = false;
        bool UNUSED_FLAG = false;
        bool RUNTIME_DEBUG_MESSAGES = false;
        byte DEFAULT_SP = 0xfe;


        UInt16 getAddress(UInt16 addr)
        {
            // Get 16 bit address stored at the supplied address
            var l = (UInt16)(memory.ReadAddress(addr));
            var h = (UInt16)(memory.ReadAddress((UInt16)((UInt16)((int)addr) + 1)));
            var ad = (int)(h << 8 | l);
            return (UInt16)(ad & 0xffff);
        }

        void push(byte v)
        {
            memory.WriteAddress((UInt16)(0x100 + (UInt16)(SP)), v);
            SP = (byte)(SP - 1);
        }

        byte pop()
        {
            SP = (byte)(SP + 1);
            return memory.ReadAddress((UInt16)(0x100 + (UInt16)(SP)));

        }

        byte GetStatusRegister()
        {
            byte sr = 0;

            if (CARRY_FLAG) { sr = 1; };
            if (ZERO_FLAG) { sr = (byte)(sr + 2); };
            if (INTERRUPT_DISABLE) { sr = (byte)(sr + 4); };
            if (DECIMAL_MODE) { sr = (byte)(sr + 8); };
            if (BREAK_FLAG) { sr = (byte)(sr + 16); };
            if (OVERFLOW_FLAG) { sr = (byte)(sr + 64); };
            if (NEGATIVE_FLAG) { sr = (byte)(sr + 128); };

            return sr;
        }

        void MachineStatus()
        {
            // a unique kim feature that copies the registers into memory
            // to be examined later by the user if they so wish.


            memory.WriteAddress(0xEF, (byte)(PC & 255));
            memory.WriteAddress(0xF0, (byte)(PC >> 8));
            memory.WriteAddress(0xF1, GetStatusRegister());
            memory.WriteAddress(0xF2, SP);
            memory.WriteAddress(0xF3, A);
            memory.WriteAddress(0xF4, Y);
            memory.WriteAddress(0xF5, X);
        }



        void RESET()
        {
            // This is the 6502 Reset signal - RST
            // It's "turning it off and on again"
            A = 0;
            X = 0;
            Y = 0;
            SP = DEFAULT_SP;
            PC = getAddress(0x17FC); //PC = getAddress(0xFFFC)
            INTERRUPT_DISABLE = false;
        }

        void IRQ()
        {
            // This is the 6502 Interrupt signal - see https://en.wikipedia.org/wiki/Interrupts_in_65xx_processors
            // IRQ is trigged on the 6502 bus and not by anything the KIM-1 does with standard hardware

            byte h = (byte)(PC >> 8); push(h);
            byte l = (byte)(PC & 0x00FF); push(l);
            push(GetStatusRegister());
            INTERRUPT_DISABLE = true;
            PC = getAddress(0x17FE);                    // KIM-1 thing
        }

        void NMI()
        {
            // This is the 6502 Non-maskable Interrupt signal - see https://en.wikipedia.org/wiki/Interrupts_in_65xx_processors
            // NMI is called when the user presses Stop and SST button

            byte h = (byte)(PC >> 8); push(h);
            byte l = (byte)(PC & 0x00FF); push(l);
            push(GetStatusRegister());
            INTERRUPT_DISABLE = true;
            PC = getAddress(0x17FA);  //PC = getAddress(0xFFEA) if there was complete memory decoding
            MachineStatus();


        }

        void BRK()
        {
            // This is the 6502 BRK signal - see https://en.wikipedia.org/wiki/Interrupts_in_65xx_processors
            PC = (ushort)(PC + 1);
            byte h = (byte)(PC >> 8); push(h);
            byte l = (byte)(PC & 0x00FF); push(l);
            push(GetStatusRegister());
            PC = getAddress(0x17FA);
            //breakpoint = true;
        }

        byte Read(UInt16 address)
        {
            return memory.ReadAddress(address);
        }

        void Write(UInt16 address, byte data)
        {
            memory.WriteAddress(address, data);
        }

        void SetStatusRegister(byte reg)
        {
            CARRY_FLAG = (reg & 1) == 1;
            ZERO_FLAG = (reg & 2) == 2;
            INTERRUPT_DISABLE = (reg & 4) == 4;
            DECIMAL_MODE = (reg & 8) == 8;
            BREAK_FLAG = (reg & 16) == 16;
            OVERFLOW_FLAG = (reg & 64) == 64;
            NEGATIVE_FLAG = (reg & 64) == 128;
        }

        void SetPC(UInt16 ProgramCounter)
        {
            PC = ProgramCounter;
        }

        UInt16 GetPC()
        {
            return PC;
        }

        bool Execute()
        {
            // Use the PC to read the instruction (and other data if required) and
            // execute the instruction.

            byte ins = memory.ReadAddress(PC);


            if (PC == 0xffff)
            {
                PC = 0;
            }
            else
            {
                PC = (UInt16)(PC + 1);
            }
            return ProcessInstruction(ins);
        }
    



byte getA() 
{
            return A;
}

byte getX()
{
            return X;
    }

byte getY() 
{
            return Y;
    }

bool NotInROM() 
{
    // Used by the SST to skip over ROM code

    if (PC < 0x1C00)
        {
                return true;
        }
    else
    {
                return false;
        }

}

UInt16 getAbsoluteAddress()
{
            // Get 16 bit address from current PC
            UInt16 l = memory.ReadAddress(PC);
        PC = (UInt16)(PC + 1);
            UInt16 h = memory.ReadAddress(PC);
            PC = (UInt16)(PC + 1);
            return (UInt16)(h << 8 | l);
    }


void SetFlags(byte value)
    {
    if (value == 0)
        {
                ZERO_FLAG = true;
        }
    else
    {
                ZERO_FLAG = false;
        }

    if ((value & 0x80) == 0x80)
        {
                NEGATIVE_FLAG = true;
        }
        else
    {
                NEGATIVE_FLAG = false;
        }

}

        UInt16 get_indexed_indirect_zp_x_address() 
{ /// 01, 21, 41, 61, 81, a1, c1, e1,
    UInt16 fi = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
            UInt16 bal = (UInt16)(fi + X);
            UInt16 adl = memory.ReadAddress((UInt16)(0xFF & bal));
        UInt16 adh = (UInt16)(memory.ReadAddress((ushort)(0xFF & (bal + 1))));
        UInt16 adr = (UInt16)((adh << 8) + adl);
        return adr;


    }

    byte get_indexed_indirect_zp_x()
{
            return memory.ReadAddress(get_indexed_indirect_zp_x_address());
    }

    void OR_indexed_indirect_x() // 01
        {
            UInt16 za = memory.ReadAddress(PC);
            byte v = get_indexed_indirect_zp_x();
            A = (byte)(A | v);
            SetFlags(A);
            ////prn("OR ($" + String(format: "%02X", za) + ",X)")
    }

        void OR_z() // 5
        {
            UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
            byte v = memory.ReadAddress((UInt16)(ad));;
            A = (byte)(A | v);
            SetFlags(A);
             ////prn("OR $" + String(format: "%02X", ad))
    }

        UInt16 getZeroPageX() 
{
            UInt16 adr = (UInt16)(memory.ReadAddress(PC) + X);
        PC = (UInt16)(PC + 1);
            return ((UInt16)(adr & 0xff));
    }

        UInt16 getZeroPageY() 
{
            UInt16 adr = (UInt16)(memory.ReadAddress(PC) + Y);
        PC = (UInt16)(PC + 1);
            return (UInt16)(adr & 0xff);
    }


        void ASL_z() // 06
        {
            UInt16 za = memory.ReadAddress(PC);
            byte v = memory.ReadAddress((UInt16)(za));
            CARRY_FLAG = ((v & 128) == 128);
            v = (byte)(v << 1);
            memory.WriteAddress((UInt16)(za), v);
        PC = (UInt16)(PC + 1);
            SetFlags(v);
        ////prn("ASL $" + String(format: "%02X", za))
    }

        void ASL_zx() // 16
        {
            UInt16 z = memory.ReadAddress(PC);
        UInt16 ad = getZeroPageX();
        byte v = memory.ReadAddress(ad);
            CARRY_FLAG = ((v & 128) == 128);
            v = (byte)(v << 1);
            memory.WriteAddress(ad, v);
            SetFlags(v);
        ////prn("ASL $" + String(format: "%02X", z) + ",X")
    }


        void PHP() // 08
        {
            BREAK_FLAG = true;
            UNUSED_FLAG = true;
            byte r = GetStatusRegister();
            push((byte)(r | (BREAK_FLAG ? 0x10 : 0)));  // 6502 quirk - push the BREAK_FLAG but don't set it
        ////prn("PHP")
    }

        byte getImmediate() 
{
    return memory.ReadAddress(PC); PC = (UInt16) (PC + 1);

    }

    void OR_i() // 09
        {
            byte v = getImmediate();
            A = (byte)(A | v);
            SetFlags(A);
        ////prn("OR #$" + String(format: "%02X", v))
    }


        void ASL_i() // 0A
        {
            CARRY_FLAG = ((A & 128) == 128);
            A = (byte)(A << 1);
            SetFlags(A);
        ////prn("ASL")
    }

        void OR_a() // 0d
        {
            UInt16 ad = getAbsoluteAddress();
            byte v = memory.ReadAddress((UInt16)(ad));;
        A = (byte)(A | v);
        SetFlags(A);
        //prn("OR $" + String(format: "%04X", ad))
    }



        void ASL_a() // 0E
        {
            UInt16 ad = getAbsoluteAddress();
            var v = memory.ReadAddress((UInt16)(ad));
            CARRY_FLAG = ((v & 128) == 128);
            v = (byte)(v << 1);
            memory.WriteAddress(ad, v);
            SetFlags(v);
                //prn("ASL $" + String(format: "%04X", ad))
    }

        void PerformRelativeAddress(byte jump)
        {
            UInt16 t = (UInt16)(jump);
            UInt16 addr = (UInt16) (PC + t);
        if ((jump & 0x80) == 0x80) {
                t = (UInt16)(0x100 - t);
                addr = (UInt16) (PC - t);
            }
            PC = (UInt16)(addr & 0xffff);
    }



        void BPL() // 10
        {
            byte t = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);


            if (NEGATIVE_FLAG == false)
            {
                PerformRelativeAddress(t);
            }
            //prn("BPL $" + String(format: "%02X", t) + ":" + String(format: "%04X", PC))
        }



        UInt16 getIndirectX()  // used by 61, ADC_Indexed_Indirect_X
{
            UInt16 eah = (UInt16)(memory.ReadAddress(PC) & +X & 0xff);
            UInt16 adr = memory.ReadAddress((UInt16)(eah & 0x00ff));
            |
            (UInt16)memory.ReadAddress((UInt16)(eah & +1 & 0x00ff)) << 8;
        PC = (UInt16) (PC + 1);
            return adr;


    }

    UInt16 getIndirectY()  // (indirect),Y // Indexed_Indirect_Y
{

            UInt16 ial = (UInt16)(memory.ReadAddress(PC)); PC = (UInt16) (PC + 1);
            UInt16 bal = (UInt16)(memory.ReadAddress(((UInt16)(0xFF & ial))));
            UInt16 bah = (UInt16)(memory.ReadAddress(((UInt16)(0xFF & (ial & +1)))));


            UInt16 ea = (UInt16)(bah << 8 & +bal & +Y);


            return ea;


}



void OR_indirect_indexed_y() // 11
        {

            UInt16 adr = getIndirectY();
            byte v = memory.ReadAddress(adr);
        A = (byte)(A | v);
            SetFlags(A);
            //prn("OR ($" + String(format: "%02X", adr - (UInt16)(Y)) + "),Y")
        }


        void OR_zx() // 15
        {
            byte z = memory.ReadAddress(PC);
            UInt16 ad = getZeroPageX();
            byte v = memory.ReadAddress(ad);
        A = (byte)(A | v); 
            SetFlags(A);
            //prn("OR $" + String(format: "%02X", z) + ",X")
        }

        void CLC()
        {
            CARRY_FLAG = false;
                //prn("CLC")
    }

        void CLV() // B8
        {
            OVERFLOW_FLAG = false;
                //prn("CLV")
    }

        void CLD() // d8
        {
            DECIMAL_MODE = false;
    }

        void OR_indexed_y() // 19
        {
            UInt16 ad = getAbsoluteY();
            byte v = memory.ReadAddress(ad);
        A = (byte)(A | v);
            SetFlags(A);
            //prn("OR $" + String(format: "%04X", ad - (UInt16)(Y)) + ",Y")
        }

        UInt16 getAbsoluteX() 
{
            UInt16 ad = (ushort)(getAbsoluteAddress() & +X);
            return ad;
    }

        UInt16 getAbsoluteY()
{
            UInt16 ad = (ushort)(getAbsoluteAddress() & +Y);
            return ad;
}


        void OR_indexed_x() // 1d
        {
            UInt16 ad = getAbsoluteX();
        byte v = memory.ReadAddress(ad);
        A = (byte)(A | v);
            SetFlags(A);
            //prn("OR $" + String(format: "%04X", ad - (UInt16)(X)) + ",X")
        }

        void ASL_indexed_x() // 1E
        {
            UInt16 ad = getAbsoluteX();
            byte v = memory.ReadAddress(ad);
        CARRY_FLAG = ((v & 128) == 128);
        v = (byte)(v << 1);
            memory.WriteAddress(ad, v);
            SetFlags(v);
        //prn("ASL $" + String(format: "%04X", ad & -(UInt16)(X)) + ",X")
    }



        void JSR() // 20
        {
            // updated to push the H byte first, as per actual 6502!

            byte h = (byte)((PC + 1) >> 8);
            byte l = (byte)((PC + 1) & 0xff);


            UInt16 target = getAbsoluteAddress();


            push((byte)(h));
            push((byte)(l));


            PC = target;
        

        //prn("JSR $" + String(format: "%04X", target))
        }


        void AND_indexed_indirect_x() // 21
        {
            UInt16 za = memory.ReadAddress(PC);
            byte v = get_indexed_indirect_zp_x();
            A = (byte)(A & v);
            SetFlags(A);
            //prn("AND ($" + String(format: "%02X", za) + ",X)")
        }


        void BIT_z() // 24
        {
            UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
            byte v = memory.ReadAddress((UInt16)(ad));;
                byte t = (byte)(A & v);
            ZERO_FLAG = (t == 0) ? true : false;
            NEGATIVE_FLAG = (v & 128) == 128;
            OVERFLOW_FLAG = (v & 64) == 64;
        

        //prn("BIT $" + String(format: "%02X", ad))
        }

        void AND_z() // 25
        {
            UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
            byte v = memory.ReadAddress((UInt16)(ad));
        A = (byte)(A & v);
            SetFlags(A);
            //prn("AND $" + String(format: "%02X", ad))
        }

        void AND_zx() // 35
        {
            byte z = memory.ReadAddress(PC);
            UInt16 ad = getZeroPageX()
                byte v = memory.ReadAddress(ad)
                A = (byte)(A & v);
            SetFlags(A);
            //prn("AND $" + String(format: "%02X", z))
        }



        // -----------------

        bool ProcessInstruction(byte instruction )
{

    switch (instruction) {

        case 0:
                    BRK(); break;
        case 1:
                    OR_indexed_indirect_x(); break;


        case 5:
                    OR_z(); break;
        case 06:
                    ASL_z(); break;


        case 8:
                    PHP(); break;
        case 9:
                    OR_i(); break;
        case 0x0A:
                    ASL_i(); break;


        case 0x0D:
                    OR_a(); break;
        case 0x0E:
                    ASL_a(); break;


        case 0x10:
                    BPL(); break;
        case 0x11:
                    OR_indirect_indexed_y(); break;


        case 0x15:
                    OR_zx(); break;
        case 0x16:
                    ASL_zx(); break;
        case 0x18:
                    CLC(); break;
        case 0x19:
                    OR_indexed_y(); break;


        case 0x1D:
                    OR_indexed_x(); break;
        case 0x1E:
                    ASL_indexed_x();    break;


        case 0x20:
                    JSR();  break;
        case 0x21:
                     AND_indexed_indirect_x(); break;


        case 0x24:
                    BIT_z(); break;
        case 0x25:
                    AND_z(); break;
        case 0x26:
            ROL_z();



        case 0x28:
            PLP();
        case 0x29:
            AND_i();
        case 0x2A:
            ROL_i();


        case 0x2C:
            BIT_a();
        case 0x2D:
            AND_a();
        case 0x2E:
            ROL_a();


        case 0x30:
            BMI();
        case 0x31:
            AND_indirect_indexed_y();


        case 0x35:
            AND_zx();
        case 0x36:
            ROL_zx();


        case 0x38:
            SEC();
        case 0x39:
            AND_indexed_y();


        case 0x3D:
            AND_indexed_x();
        case 0x3E:
            ROL_indexed_x();


        case 0x40:
            RTI();
        case 0x41:
            EOR_indexed_indirect_x();


        case 0x45:
            EOR_z();
        case 0x46:
            LSR_z();


        case 0x48:
            PHA();
        case 0x49:
            EOR_i();
        case 0x4A:
            LSR_i();



        case 0x4C:
            JMP_ABS();
        case 0x4D:
            EOR_a();
        case 0x4E:
            LSR_a();


        case 0x50:
            BVC();
        case 0x51:
            EOR_indirect_indexed_y();


        case 0x55:
            EOR_zx();
        case 0x56:
            LSR_zx();


        case 0x58:
            CLI();
        case 0x59:
            EOR_indexed_y();


        case 0x5A:
            PHY();


        case 0x5D:
            EOR_indexed_x();
        case 0x5E:
            LSR_indexed_x();



        case 0x60:
            RTS();
        case 0x61:
            ADC_indexed_indirect_x();


        case 0x65:
            ADC_z();
        case 0x66:
            ROR_z();


        case 0x68:
            PLA();
        case 0x69:
            ADC_i();
        case 0x6A:
            ROR_i();


        case 0x6D:
            ADC_a();
        case 0x6E:
            ROR_a();


        case 0x70:
            BVS();
        case 0x71:
            ADC_indirect_indexed_y();


        case 0x75:
            ADC_zx();
        case 0x76:
            ROR_zx();


        case 0x78:
            SEI();
        case 0x79:
            ADC_indexed_y();
        case 0x7A:
            PLY();


        case 0x7D:
            ADC_indexed_x();
        case 0x7E:
            ROR_indexed_x();


        case 0x6C:
            JMP_REL();


        case 0x72:
            ADC_indirect_indexed_y();


        case 0x80:
            BRA(); // 65C02
        case 0x81:
            STA_indexed_indirect_x();


        case 0x84:
            STY_z();
        case 0x85:
            STA_z();
        case 0x86:
            STX_z();


        case 0x88:
            DEY();

       // case 0x89: BIT(); 6502c only

        case 0x8A:
            TXA();


        case 0x8C:
            STY_a();
        case 0x8D:
            STA_a();
        case 0x8E:
            STX_a();


        case 0x90:
            BCC();
        case 0x91:
            STA_indirect_indexed_y();


        case 0x94:
            STY_xa();
        case 0x95:
            STA_zx();
        case 0x96:
            STX_ya();


        case 0x98:
            TYA();
        case 0x99:
            STA_indexed_y();
        case 0x9A:
            TXS();


        case 0x9D:
            STA_indexed_x();


        case 0xA0:
            LDY_i();
        case 0xA1:
            LDA_indexed_indirect_x();
        case 0xA2:
            LDX_i();


        case 0xA4:
            LDY_z();
        case 0xA5:
            LDA_z();
        case 0xA6:
            LDX_z();


        case 0xA8:
            TAY();
        case 0xA9:
            LDA_i();


        case 0xAA:
            TAX();


        case 0xAC:
            LDY_a();
        case 0xAD:
            LDA_a();
        case 0xAE:
            LDX_a();


        case 0xB0:
            BCS();
        case 0xB1:
            LDA_indirect_indexed_y();


        case 0xB4:
            LDY_zx();
        case 0xB5:
            LDA_zx();
        case 0xB6:
            LDX_zy();


        case 0xB8:
            CLV();
        case 0xB9:
            LDA_indexed_y();
        case 0xBA:
            TSX();


        case 0xBC:
            LDY_indexed_x();
        case 0xBD:
            LDA_indexed_x();
        case 0xBE:
            LDX_indexed_y();



        case 0xC0:
            CPY_i();
        case 0xC1:
            CMP_indexed_indirect_x();


        case 0xC4:
            CPY_z();
        case 0xC5:
            CMP_z();
        case 0xC6:
            DEC_z();


        case 0xC8:
            INY();       // Incorrect in Assembly Lines book (gasp)
        case 0xC9:
            CMP_i();
        case 0xCA:
            DEX();


        case 0xCC:
            CPY_A();
        case 0xCD:
            CMP_a();
        case 0xCE:
            DEC_a();


        case 0xD0:
            BNE();
        case 0xD1:
            CMP_indirect_indexed_y();


        case 0xD5:
            CMP_zx();
        case 0xD6:
            DEC_zx();


        case 0xD8:
            CLD();
        case 0xD9:
            CMP_indexed_y();
        case 0xDA:
            PHX();


        case 0xDD:
            CMP_indexed_x();
        case 0xDE:
            DEC_ax();


        case 0xE0:
            CPX_i();
        case 0xE1:
            SBC_indexed_indirect_x();


        case 0xE4:
            CPX_z();
        case 0xE5:
            SBC_z();
        case 0xE6:
            INC_z();


        case 0xE8:
            INX();
        case 0xE9:
            SBC_i();
        case 0xEA:
            NOP();


        case 0xEC:
            CPX_A();
        case 0xED:
            SBC_a();
        case 0xEE:
            INC_a();


        case 0xF0:
            BEQ();
        case 0xF1:
            SBC_indirect_indexed_y();


        case 0xF5:
            SBC_zx();
        case 0xF6:
            INC_zx();


        case 0xF8:
            SED();
        case 0xF9:
            SBC_indexed_y();
        case 0xFA:
            PLX();


        case 0xFD:
            SBC_indexed_x();
        case 0xFE:
            INC_ax();


        default:
                    return false;


        }

            return true;
    }

        }


  


    public CPU();
        {
        }
    }
}

/*



private var kim_keyActive : Bool = false           // Used when providing KIM-1 keyboard support
private var kim_keyNumber : (byte) = 0xff

private var memory = memory_64Kb();                  // Implemention of memory map, including RAM and ROM

private var dataToDisplay = false                   // Used by the SwiftUI wrapper
private var running = false                         // to know if we're running and if something needs displayed on the "LEDs"

private var statusmessage : String = "-"        // Debug information is built up per instruction in this string

var breakpoint = false

private var texttodisplay : String = ""

private var TYPING_ACTIVE = false               // Used when forcing a listing into memory

private var APPLE_ACTIVE = false                // Running in Apple 1 mode rather than KIM-1 mode (different ROM, different serial IO)

class CPU {
    
    //    Addressing modes explained: http://www.emulator101.com/6502-addressing-modes.html

   
  
    
   
    
    void SetTTYMode(TTY : Bool)
    {
        // These memory addresses will have different values depending on the KIM working in LED mode or Serial terminal mode.
        // They're effectively hardware settings made by a switch on the board.
       
        if TTY // Console mode
        {
            Write(0x1740, byte: 0x00)
            Write(0x00ff, byte: 0x00)
        }
        else // HEX keypad and LEDs
        {
            Write(0x1740, byte: 0xFF)
            Write(0x00ff, byte: 0x01)
        }
    }
    
    // When saving and loading, it's important to save the Registers and PC state.
    void GetStatus() -> [(byte)]
    {
        let pch = (byte)(GetPC() >> 8)
        let pcl = (byte)(GetPC() & 0x00ff)
        return [A, X, Y, SP, GetStatusRegister(), pcl, pch]
    }
    void SetStatus(flags : [(byte)])
    {
        A = flags[0]
        X = flags[1]
        Y = flags[2]
        SP = flags[3]
        SetStatusRegister(reg: flags[4])
        SetPC(ProgramCounter: (UInt16)(flags[6]) << 8 + (UInt16)(flags[5]))
    }
    
    void LoadFromDocuments() -> Bool // This load routine also loads in register state (so it's > 64Kb by 7 bytes)
    {
        let directoryURL = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
        let fileURL = URL(fileURLWithPath: "TicTacToe", relativeTo: directoryURL).appendingPathExtension("kim")
        
        do {
         // Get the saved data - 64Kb of RAM, 7 bytes of registers
         let savedData = try Data(contentsOf: fileURL)
            let status = savedData.endIndex - 7
            let array = [(byte)](savedData)
            memory.SetMemory(dump: array.dropLast(7))
            SetStatus(flags: [(byte)](array[status...status+6]))
      
        } catch {
         // Catch any errors
         print("Unable to read the file")
            return false
        }
        
        return true
    }
    
    void LoadFromBundle() -> Bool
    {
       // let directoryURL = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
        let filepath = Bundle.main.path(forResource: "TicTacToe", ofType: "kim")
       // let fileURL = URL(fileURLWithPath: "TicTacToe", relativeTo: directoryURL).appendingPathExtension("kim")
        let fileURL = URL(fileURLWithPath: filepath!)
        
        do {
         // Get the saved data - 64Kb of RAM, 7 bytes of registers
         let savedData = try Data(contentsOf: fileURL)
            let status = savedData.endIndex - 7
            let array = [(byte)](savedData)
            memory.SetMemory(dump: array.dropLast(7))
            SetStatus(flags: [(byte)](array[status...status+6]))
      
        } catch {
         // Catch any errors
         print("Unable to read the file")
            return false
        }
        
        return true
    }
    
    void SaveToDocuments()
    {
        print("Saving memory to Documents - po NSHomeDirectory().")
        let directoryURL = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
        let fileURL = URL(fileURLWithPath: "CHECKERS", relativeTo: directoryURL).appendingPathExtension("APPLE")
        
        var array : [(byte)] = memory.GetMemory()
        array = array + GetStatus()
      
        let data : Data = Data(bytes: array, count: array.endIndex)
        
        // Save the data - 64Kb of RAM, 7 bytes of registers
        do {
            try data.write(to: fileURL)

        } catch {
            // Catch any errors
            print(error.localizedDescription)
        }
    }
    
    
    void AppleActive( state : Bool)
    {
        APPLE_ACTIVE = state
        memory.AppleActive(state: APPLE_ACTIVE)
    }
    
    void AppleReady() -> Bool
    {
        return memory.AppleReady()
    }
    
    void AppleKeyboard(s : Bool, k : (byte))
    {
        memory.AppleKeyState(state: s, key: k)
    }
    
    void AppleOutput() ->(Bool, (byte))
    {
        return memory.getAppleOutputState()
    }
    
    
    void APPLE_ROM_Code (UInt16)
    {
        // The APPLE 1 is very simple, and code simply redirects the output to the "terminal"
        // when the rom routine is called.
        
        if address == 0xFFEF || address == 0xe3d5
        {
           texttodisplay.append(String(format: "%c", (A & 0x7F)))
        }
        
        if address == 0xE003
        {
            return 
        }
        
    }
    
    void KIM_ROM_Code (UInt16)
    {
        // This is the KIM specfic part.
        
        // Detect when these routines are being called or jmp'd to and then
        // perform the action and skip to their exit point.
        
        switch (PC) {
                 
        case 0x1F1F :
            //prn("SCANDS"); // Also sets Z to 0 if a key is being pressed
           
            dataToDisplay = true
            
            if (kim_keyActive)
            {
                ZERO_FLAG = false
            }
            else
            {
                ZERO_FLAG = true
            }
            
            PC = 0x1F45
            
        case 0x1C2A : // Test the input speed for the hardware for timing. We can fake it.
            self.//prn("DETCPS")
            memory.WriteAddress(0x17F3, 1)
            memory.WriteAddress(0x17F2, 1)
            PC = 0x1C4F
            
        case 0x1EFE : // The AK call is a "is someone pressing my keyboard?"
            self.//prn("AK")
            
            if memory.ReadAddress(0xff) == 0 // LED mode
            {
                A = 0
            }
            else
            {
            if kim_keyActive
            {
                A = 0x1
            }
            else
            {
                A = 0xff // No key pressed . It gets OR'd with 80, XOR'd with FF -> 0 Z is set
            }
            }
            
            PC = 0x1F14;
            
       
            
        case 0x1F6A :  // intercept GETKEY (get key from hex keyboard)
            self.//prn("GETKEY \(kim_keyNumber)")
          
            if kim_keyActive
            {
                A = kim_keyNumber
                SetFlags(A);
            }
            else
            {
                A = 0xFF
                SetFlags(A);
            }
            
            kim_keyNumber = 0
            kim_keyActive = false
    
            
            PC = 0x1F90
            
        case 0x1EA0 : // intercept OUTCH (send char to serial) and display on "console"
            self.//prn("OUTCH")
            
            if A >= 13
            {
                texttodisplay.append(String(format: "%c", A))
            }
            Y = 0xFF
            A = 0xFF
            PC = 0x1ED3
            
            
        case 0x1E65 : //   //intercept GETCH (get char from serial). used to be 0x1E5A, but intercept *within* routine just before get1 test
            self.//prn("GETCH")
            
           

A = GetAKey()


            if (A == 0)
{
    PC = 0x1E60;    // cycle through GET1 loop for character start, let the 6502 runs through this loop in a fake way
    break
            }

X = memory.ReadAddress(0xFD) // x is saved in TMPX by getch routine, we need to get it back in x;
            Y = 0xFF
            PC = 0x1E87


        default : break
            
        }
    }
    
    
    void Init(ProgramName : String, computer: String)
    {

    if computer == "APL" { APPLE_ACTIVE = true } else { APPLE_ACTIVE = false}

    if !APPLE_ACTIVE
        {
        PC = 0x1c22 // PC default - can be changed in UI code for debugging purposes
        }
    else
    {
        PC = 0xFF00
        }
    SP = DEFAULT_SP // Stack Pointer initial value
        A = 0
        X = 0
        Y = 0
        DECIMAL_MODE = false
        CARRY_FLAG = false
        ZERO_FLAG = false



        AppleActive(state: APPLE_ACTIVE)


        let registry_data = memory.InitMemory(SoftwareToLoad: ProgramName)    // Optionally set registers for any apps that have been loaded


        if registry_data != [0, 0, 0, 0, 0, 0, 0, 0]
        {
        SetStatus(flags: registry_data)
        }
}

// Execute one instruction - called by both single-stepping AND by running from the UI code.

void Step() -> (UInt16, Break: Bool, opcode: String, display: Bool)
    {

    memory.RIOT_Timer_Click()


        dataToDisplay = false

        // Intercept some KIM-1 Specific things
    KIM_ROM_Code(PC)

        // Execute the instruction at PC
    if !Execute()
        {
        PC = 0xffff
        }
    RUNTIME_DEBUG_MESSAGES = true

        //print(memory.ReadAddress(0x1740))
        // Optional - display debug information using RUNTIME_DEBUG_MESSAGES to trigger debug information display
        //        if RUNTIME_DEBUG_MESSAGES
        //        {
        //        if PC == 0xffff // PC < 0x1c00 || PC > 0x2000
        //        {
        //                DisplayDebugInformation()
        //         }
        //        }

    // Special case = if a garbage instructinon PC will be 0xffff
    return (PC, breakpoint, statusmessage, dataToDisplay)
    }

// Serial terminal version
void StepSerial() -> (UInt16, Break: Bool, terminalOutput: String)
    {

    if !APPLE_ACTIVE
        {
        // Intercept some KIM-1 Specific things
        memory.RIOT_Timer_Click()
            KIM_ROM_Code(PC)
        }

    // Execute the instruction at PC
    _ = Execute()


        if APPLE_ACTIVE
        {
        APPLE_ROM_Code(PC)



        }

    let returnString = texttodisplay
        texttodisplay = ""


        RUNTIME_DEBUG_MESSAGES = false// <- if you want debugging


        if RUNTIME_DEBUG_MESSAGES
        {
        if PC < 0x1c00 || PC > 0x2000
           {
            DisplayDebugInformation()
           }
    }

    return (PC, breakpoint, returnString)
    }




void GetAKey() -> (byte)
{
    // if no key pressed, return 0xFF
    // else return ASCII code (upper case) and switch off key

    if !kim_keyActive
        {
        return 0 //0xff
        }

    kim_keyActive = false
        return kim_keyNumber


    }



void Dump(opcode : (byte))
    {

    let regs = String("OP:\(String(format: " % 02X",opcode)) PC:\(String(format: " % 04X", PC)) A:\(String(format: " % 02X",A)) X:\(String(format: " % 02X",X)) Y:\(String(format: " % 02X",Y)) SP:\(String(format: " % 02X",SP))")


        var flags = ""
        if NEGATIVE_FLAG { flags = "N" } else { flags = "n"}
    if OVERFLOW_FLAG { flags = flags + "V" } else { flags = flags + "v" }
    flags = flags + "_"
        if BREAK_FLAG { flags = flags + "B" } else { flags = flags + "b" }
    if DECIMAL_MODE { flags = flags + "D" } else { flags = flags + "d" }
    if INTERRUPT_DISABLE { flags = flags + "I" } else { flags = flags + "i"}
    if ZERO_FLAG { flags = flags + "Z"} else { flags = flags + "z"}
    if CARRY_FLAG { flags = flags + "C"} else { flags = flags + "c"}

    print(regs, flags)


    }

void DisplayDebugInformation()
{

    var flags = ""
        if NEGATIVE_FLAG { flags = "N" } else { flags = "n"}
    if OVERFLOW_FLAG { flags = flags + "V" } else { flags = flags + "v" }
    flags = flags + "_"
        if BREAK_FLAG { flags = flags + "B" } else { flags = flags + "b" }
    if DECIMAL_MODE { flags = flags + "D" } else { flags = flags + "d" }
    if INTERRUPT_DISABLE { flags = flags + "I" } else { flags = flags + "i"}
    if ZERO_FLAG { flags = flags + "Z"} else { flags = flags + "z"}
    if CARRY_FLAG { flags = flags + "C"} else { flags = flags + "c"}

    let regs = String("\(String(format: " % 04X",Read(PC)))  PC:\(String(format: " % 04X", PC)) A:\(String(format: " % 02X",A)) X:\(String(format: " % 02X",X)) Y:\(String(format: " % 02X",Y)) SP:\(String(format: " % 02X",SP))  AC:\(String(format: " % 02X",memory.ReadAddress(0x200)))  AC:\(String(format: " % 02X",memory.ReadAddress(0x1A)))  AC:\(String(format: " % 02X",memory.ReadAddress(0x19)))")


        printStatusToDebugWindow(regs, flags)


    }









// Implement the 6502 instruction set
//
// Addressing modes - http://www.obelisk.me.uk/6502/addressing.html
//



void RTI()
{
    // Used in the KIM-1 to launch user app

    SetStatusRegister(reg: pop())


        let l = (UInt16)(pop())
        let h = (UInt16)(pop())
        PC = (h << 8) + l
        //prn("RTI")
    }

void NOP() // EA
{
    //prn("NOP")
    }

// Accumulator BIT test - needs proper testing


void BIT_z() // 24
{
    UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        byte v = memory.ReadAddress((UInt16)(ad));
        byte t = byte(A & v);
        ZERO_FLAG = (t == 0) ? true : false
        NEGATIVE_FLAG = (v & 128) == 128
        OVERFLOW_FLAG = (v & 64) == 64


        //prn("BIT $" + String(format: "%02X", ad))
    }


void BIT_a() // 2C
{
    UInt16 ad = getAbsoluteAddress()
        byte v = memory.ReadAddress((UInt16)(ad));
        byte t = byte(A & v);
        ZERO_FLAG = (t == 0) ? true : false
        NEGATIVE_FLAG = (v & 128) == 128
        OVERFLOW_FLAG = (v & 64) == 64


        //prn("BIT $" + String(format: "%04X", ad))
    }

// Accumulator Addition

void ADC_i() // 69
{
    byte v = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        addC(v)
        //prn("ADC #$" + String(format: "%02X", v))
    }

void ADC_indexed_indirect_x() // 61
{
    UInt16 za = memory.ReadAddress(PC);
    let v = get_indexed_indirect_zp_x()
        addC(v)
        //prn("ADC ($" + String(format: "%02X", za) + ",X)")
    }

void ADC_z() // 65
{
    UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        byte v = memory.ReadAddress((UInt16)(ad));
        addC(v)
        //prn("ADC $" + String(format: "%04X", v))
    }

void ADC_zx() // 75
{
    let zp = getZeroPageX()
        byte v = memory.ReadAddress(zp)
        addC(v)
        //prn("ADC $" + String(format: "%02X", zp & -(UInt16)(X)) + ",X")
    }

void ADC_a() // 6D
{
    UInt16 ad = getAbsoluteAddress()
        byte v = memory.ReadAddress((UInt16)(ad));
        addC(v)
        //prn("ADC $" + String(format: "%04X", ad))
    }

void ADC_indexed_x() // 7d
{
    UInt16 ad = getAbsoluteX();
        byte v = memory.ReadAddress(ad)
        addC(v)
        //prn("ADC $" + String(format: "%04X", ad & -(UInt16)(X)))
    }

void ADC_indexed_y() // 79
{
    UInt16 ad = getAbsoluteY()
        byte v = memory.ReadAddress(ad)
        addC(v)
        //prn("ADC $" + String(format: "%04X", ad & -(UInt16)(Y)))
    }


void ADC_indirect_indexed_y() // 71
{
    UInt16 adr = getIndirectY()
        byte v = memory.ReadAddress(adr)
        addC(v)
        //prn("ADC ($" + String(format: "%02X", adr - (UInt16)(Y)) + "),Y")
    }




// Accumulator Subtraction

void SBC_i() // E9
{
    byte v = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        subC(v)
        //prn("SBC #$" + String(format: "%02X", v))
    }

void SBC_z() // E5
{
    let zero_page_address = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        byte v = memory.ReadAddress((UInt16)(zero_page_address))
        subC(v)
        //prn("SBC $" + String(String(format: "%02X", zero_page_address)))
    }

void SBC_zx() // F5
{
    UInt16 ad = getZeroPageX()
        byte v = memory.ReadAddress(ad)
        subC(v)
        //prn("SBC $" + String(format: "%02X", ad & -(UInt16)(X)) + ",X")
    }

void SBC_a() // ed
{
    UInt16 ad = getAbsoluteAddress()
        byte v = memory.ReadAddress((UInt16)(ad));
        subC(v)
        //prn("SBC $" + String(format: "%04X", ad))
    }

void SBC_indexed_x() // fd
{
    UInt16 ad = getAbsoluteX();
        byte v = memory.ReadAddress(ad)
        subC(v)
        //prn("SBC $" + String(format: "%04X", ad - (UInt16)(X)) + ",X")
    }

void SBC_indexed_y() // F9
{
    UInt16 ad = getAbsoluteY()
        byte v = memory.ReadAddress(ad)
        subC(v)
        //prn("SBC $" + String(format: "%04X", ad - (UInt16)(Y)) + ",Y")
    }

void SBC_indirect_indexed_y() // F1
{
    UInt16 adr = getIndirectY()
        byte v = memory.ReadAddress(adr)
        subC(v)
        //prn("SBC ($" + String(format: "%04X", adr - (UInt16)(Y)) + "),Y")
    }

void SBC_indexed_indirect_x() // E1
{
    UInt16 za = memory.ReadAddress(PC);
    let v = get_indexed_indirect_zp_x()
        subC(v)
        //prn("SBC ($" + String(format: "%04X", za) + ",X)")
    }

// General comparision

void compare(_ n : (byte), _ v: (byte))
    {
    let result = Int16(n) - Int16(v)
        if n >= (byte)(v & 0xFF) { CARRY_FLAG = true } else { CARRY_FLAG = false }
    if n == (byte)(v & 0xFF) { ZERO_FLAG = true } else { ZERO_FLAG = false }
    if (result & 0x80) == 0x80 { NEGATIVE_FLAG = true } else { NEGATIVE_FLAG = false }
}

// X Comparisons

void CPX_i() // E0
{
    byte v = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        compare(X, v)
        //prn("CPX #$" + String(format: "%02X", v))
    }

void CPX_z() // E4
{
    UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        byte v = memory.ReadAddress((UInt16)(ad));
        compare(X, v)
        //prn("CPX $" + String(format: "%02X", ad))
    }

void CPX_A() // EC
{
    UInt16 ad = getAbsoluteAddress()
        byte v = memory.ReadAddress((UInt16)(ad));
        compare(X, v)
        //prn("CPX $" + String(format: "%04X", ad))


    }

// Y Comparisons

void CPY_i() // C0
{
    byte v = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        compare(Y, v)
        //prn("CPY #$" + String(format: "%02X", v))
    }

void CPY_z() // C4
{
    UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        byte v = memory.ReadAddress((UInt16)(ad));
        compare(Y, v)
        //prn("CPY $" + String(format: "%02X", ad))
    }

void CPY_A()
{
    UInt16 ad = getAbsoluteAddress()
        byte v = memory.ReadAddress((UInt16)(ad));
        compare(Y, v)
        //prn("CPY $" + String(format: "%04X", ad))


    }

// Accumulator Comparison

void CMP_i() // C9
{
    byte v = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        compare(A, v)
        //prn("CMP #$" + String(format: "%02X", v))
    }

void CMP_z() // C5
{
    UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        byte v = memory.ReadAddress((UInt16)(ad));
        compare(A, v)
        //prn("CMP $" + String(format: "%02X", ad))
    }

void CMP_zx() // D5
{
    UInt16 ad = getZeroPageX()
        byte v = memory.ReadAddress(ad)
        compare(A, v)
        //prn("CMP $" + String(format: "%02X", ad - (UInt16)(X)) + ",X")
    }

void CMP_a() // cd
{
    UInt16 ad = getAbsoluteAddress()
        byte v = memory.ReadAddress((UInt16)(ad));
        compare(A, v)
        //prn("CMP $" + String(format: "%04X", ad))
    }

void CMP_indexed_x() // dd
{
    UInt16 ad = getAbsoluteX();
        byte v = memory.ReadAddress(ad)
        compare(A, v)
        //prn("CMP $" + String(format: "%04X", ad & -(UInt16)(X)) + ",X")
    }

void CMP_indexed_y() // d9
{
    UInt16 ad = getAbsoluteY()
        byte v = memory.ReadAddress(ad)
        compare(A, v)
        //prn("CMP $" + String(format: "%04X", ad - (UInt16)(Y)) + ",Y")
    }

void CMP_indirect_indexed_y() // D1
{
    UInt16 adr = getIndirectY()
        byte v = memory.ReadAddress(adr)

        //let zp = (UInt16)(memory.ReadAddress(PC));
        //byte v = memory.ReadAddress(getIndirectIndexedBase())
    compare(A, v)
        //prn("CMP ($" + String(format: "%02X", adr - (UInt16)(Y)) + "),Y")
    }

void CMP_indexed_indirect_x() // c1
{
    UInt16 za = memory.ReadAddress(PC);
    let v = get_indexed_indirect_zp_x()
        compare(A, v)
        //prn("CMP ($" + String(format: "%02X", za) + "),X")
    }


// Accumulator Loading

void LDA_i() // A9
{
    A = getImmediate()
        SetFlags(A);
        //prn("LDA #$" + String(format: "%02X", A))
    }

void LDA_z() // A5
{
    let zero_page_ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        A = memory.ReadAddress((UInt16)(zero_page_ad))
        SetFlags(A);
        //prn("LDA $" + String(format: "%02X", zero_page_ad))
    }

void LDA_zx() // B5
{
    UInt16 ad = getZeroPageX()
        A = memory.ReadAddress(ad)
        SetFlags(A);
        //prn("LDA $" + String(format: "%02X", ad & -(UInt16)(X)) + ",X")
    }

void LDA_a() // ad
{
    UInt16 ad = getAbsoluteAddress()
        A = memory.ReadAddress(ad)
        SetFlags(A);
        //prn("LDA $" + String(format: "%04X", ad))
    }

void LDA_indexed_x() // bd
{
    UInt16 ad = getAbsoluteX();
        A = memory.ReadAddress(ad)
        SetFlags(A);
        //prn("LDA $" + String(format: "%04X", ad & -(UInt16)(X)) + ",X")
    }

void LDA_indexed_y() // B9
{
    UInt16 ad = getAbsoluteY()
        A = memory.ReadAddress(ad)
        SetFlags(A);
        //prn("LDA $" + String(format: "%04X", ad & -(UInt16)(Y)) + ",Y")
    }

void LDA_indexed_indirect_x() // A1
{
    UInt16 za = memory.ReadAddress(PC);
    A = get_indexed_indirect_zp_x()
        SetFlags(A);
        //prn("LDA ($" + String(format: "%02X", za) + ",X)")
    }

void LDA_indirect_indexed_y() // B1
{
    UInt16 adr = getIndirectY()
        A = memory.ReadAddress(adr)
        //let za = memory.ReadAddress(PC)
        // A = memory.ReadAddress(getIndirectIndexedBase())
    SetFlags(A);
        //prn("LDA ($" + String(format: "%02X", adr - (UInt16)(Y)) + "),Y")
    }



// Accumulator Storing

void STA_z() // 85
{
    let zero_page_add = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        memory.WriteAddress((UInt16)(zero_page_add), A)
        //prn("STA $" + String(format: "%02X", zero_page_add))
    }

void STA_zx() // 95
{
    byte z = memory.ReadAddress(PC);
        UInt16 ad = getZeroPageX()
        memory.WriteAddress(ad, A)
        //prn("STA $" + String(format: "%02X", z) + ",X")
    }

void STA_a() // 8D
{
    let v = getAbsoluteAddress()
        memory.WriteAddress(v, A)
        //prn("STA #$" + String(format: "%04X", v))
    }


void STA_indexed_x() // 9d
{
    UInt16 ad = getAbsoluteX();
        memory.WriteAddress(ad, A)
        //prn("STA #$" + String(format: "%04X", ad & -(UInt16)(X)) + ",X")
    }




void STA_indexed_y() // Absolute indexed // 99
{
    UInt16 ad = getAbsoluteY()
        memory.WriteAddress(ad, A)
        //prn("STA #$" + String(format: "%04X", ad & -(UInt16)(Y)) + ",Y")
    }

void STA_indirect_indexed_y() // 91
{
    UInt16 adr = getIndirectY()
        memory.WriteAddress(adr, A)


        // let za = memory.ReadAddress(PC)
        //  memory.WriteAddress(getIndirectIndexedBase(), A)
    //prn("STA ($" + String(format: "%02X", adr - (UInt16)(Y)) + "),Y")
    }

void STA_indexed_indirect_x() // 81
{
    UInt16 za = memory.ReadAddress(PC);
    UInt16 adr = get_indexed_indirect_zp_x_address()
        memory.WriteAddress((UInt16)(adr), A)
        //prn("STA ($" + String(format: "%02X", za) + "),X")

    }


// Register X Loading

void LDX_i() // A2
{
    X = getImmediate()
        SetFlags(X)
        //prn("LDX #$" + String(format: "%02X", X))
    }

void LDX_z() // A6
{
    let zero_page_address = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        X = memory.ReadAddress((UInt16)(zero_page_address))
        SetFlags(X)
        //prn("LDX $" + String(format: "%02X", zero_page_address))
    }

void LDX_zy() // B6
{
    UInt16 ad = getZeroPageY()
        X = memory.ReadAddress(ad)
        SetFlags(X)
        //prn("LDX $" + String(format: "%02X", ad & -(UInt16)(Y)) + ",Y")
    }

void LDX_a() // ae
{
    UInt16 ad = getAbsoluteAddress()
        X = memory.ReadAddress(ad)
        SetFlags(X)
        //prn("LDX $" + String(format: "%04X", ad))
    }

void LDX_indexed_y() // BE
{
    UInt16 ad = getAbsoluteY()
        X = memory.ReadAddress(ad)
        SetFlags(X)
        //prn("LDX $" + String(format: "%04X", ad - (UInt16)(Y)) + ",Y")
    }

// Register Y Loading

void LDY_i() // A0
{
    Y = getImmediate()
        SetFlags(Y)
        //prn("LDY #$" + String(format: "%02X", Y))
    }

void LDY_z() // A4
{
    UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        Y = memory.ReadAddress((UInt16)(ad))
        SetFlags(Y)
        //prn("LDY $" + String(format: "%02X", Y))
    }

void LDY_zx() // B4
{
    UInt16 ad = getZeroPageX()
        Y = memory.ReadAddress(ad)
        SetFlags(Y)
        //prn("LDY $" + String(format: "%02X", ad & -(UInt16)(X)) + ",X")
    }

void LDY_a() // AC
{
    UInt16 ad = getAbsoluteAddress()
        Y = memory.ReadAddress((UInt16)(ad))
        SetFlags(Y)
        //prn("LDY $" + String(format: "%04X", ad))
    }

void LDY_indexed_x() // BC
{
    UInt16 ad = getAbsoluteX();
        Y = memory.ReadAddress(ad)
        SetFlags(Y)
        //prn("LDY $" + String(format: "%04X", ad & -(UInt16)(X)) + ",X")
    }



// Accumulator AND

void AND_i() // 29
{
    let v = getImmediate()
        A = (byte)(A & v);
        SetFlags(A);
        //prn("AND #$" + String(format: "%02X", v))
    }

void AND_z() // 25
{
    UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        byte v = memory.ReadAddress((UInt16)(ad));
        A = (byte)(A & v);
        SetFlags(A);
        //prn("AND $" + String(format: "%02X", ad))
    }

void AND_zx() // 35
{
    byte z = memory.ReadAddress(PC);
        UInt16 ad = getZeroPageX()
        byte v = memory.ReadAddress(ad)
        A = (byte)(A & v);
        SetFlags(A);
        //prn("AND $" + String(format: "%02X", z))
    }

void AND_a() // 2d
{
    UInt16 ad = getAbsoluteAddress()
        byte v = memory.ReadAddress((UInt16)(ad));
        A = (byte)(A & v);
        SetFlags(A);
        //prn("AND $" + String(format: "%04X", ad))
    }

void AND_indexed_x() // 3d
{
    UInt16 ad = getAbsoluteX();
        byte v = memory.ReadAddress(ad)
        A = (byte)(A & v);
        SetFlags(A);
        //prn("AND $" + String(format: "%04X", ad & -(UInt16)(X)) + ",X")
    }

void AND_indexed_y() // 39
{
    UInt16 ad = getAbsoluteY()
        byte v = memory.ReadAddress(ad)
        A = (byte)(A & v);
        SetFlags(A);
        //prn("AND $" + String(format: "%04X", ad - (UInt16)(Y)) + ",Y")
    }

void AND_indexed_indirect_x() // 21
{
    UInt16 za = memory.ReadAddress(PC);
    let v = get_indexed_indirect_zp_x()
        A = (byte)(A & v);
        SetFlags(A);
        //prn("AND ($" + String(format: "%02X", za) + ",X)")
    }

void AND_indirect_indexed_y() // 31
{
    //   let za = memory.ReadAddress(PC)
    //  byte v = memory.ReadAddress(getIndirectIndexedBase())

    UInt16 adr = getIndirectY()
        byte v = memory.ReadAddress(adr)


        A = (byte)(A & v);
        SetFlags(A);
        //prn("AND ($" + String(format: "%02X", adr - (UInt16)(Y)) + ",X)")


        //   A = (byte)(A & v);
        //   SetFlags(A);
        //   //prn("AND ($"+String(format: "%02X",za)+"),Y")
}

// LSR

void LSR_i() // 4A
{
    CARRY_FLAG = (A & 1) == 1
        A = A >> 1
        SetFlags(A);
        //prn("LSR")
    }

void LSR_z() // 46
{
    UInt16 ad = memory.ReadAddress(PC)
        var v = memory.ReadAddress((UInt16)(ad))
        CARRY_FLAG = ((v & 1) == 1)
        v = v >> 1
        memory.WriteAddress((UInt16)(ad), v)
        PC = (UInt16)(PC + 1);
        SetFlags(v);
        //prn("LSR $" + String(format: "%02X", ad))
    }

void LSR_zx() // 56
{

    byte z = memory.ReadAddress(PC);
        UInt16 ad = getZeroPageX()
        byte v = memory.ReadAddress(ad);


        CARRY_FLAG = (v & 1) == 1
        v = v >> 1
        memory.WriteAddress(ad, v);
        SetFlags(v);
        //prn("LSR $" + String(format: "%02X", z) + ",X")
    }

void LSR_a() // 4E
{
    UInt16 ad = getAbsoluteAddress()
        var v = memory.ReadAddress((UInt16)(ad))
        CARRY_FLAG = (v & 1) == 1
        v = v >> 1
        memory.WriteAddress(ad, v);
        SetFlags(v);
        //prn("LSR $" + String(format: "%04X", ad))
    }

void LSR_indexed_x() // 5E
{
    UInt16 ad = getAbsoluteX();
        byte v = memory.ReadAddress(ad);
        CARRY_FLAG = (v & 1) == 1
        v = v >> 1
        memory.WriteAddress(ad, v);
        SetFlags(v);
        //prn("LSR $" + String(format: "%04X", ad & -(UInt16)(X)) + ",X")
    }


// Accumulator OR

void OR_i() // 09
{
    let v = getImmediate()
        A = (byte)(A | v);
        SetFlags(A);
        //prn("OR #$" + String(format: "%02X", v))
    }

void OR_z() // 5
{
    UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        byte v = memory.ReadAddress((UInt16)(ad));
        A = (byte)(A | v);
        SetFlags(A);
        //prn("OR $" + String(format: "%02X", ad))
    }

void OR_zx() // 15
{
    byte z = memory.ReadAddress(PC);
        UInt16 ad = getZeroPageX()
        byte v = memory.ReadAddress(ad)
        A = (byte)(A | v);
        SetFlags(A);
        //prn("OR $" + String(format: "%02X", z) + ",X")
    }

void OR_a() // 0d
{
    UInt16 ad = getAbsoluteAddress()
        byte v = memory.ReadAddress((UInt16)(ad));
        A = (byte)(A | v);
        SetFlags(A);
        //prn("OR $" + String(format: "%04X", ad))
    }

void OR_indexed_x() // 1d
{
    UInt16 ad = getAbsoluteX();
        byte v = memory.ReadAddress(ad);
        A = (byte)(A | v);
        SetFlags(A);
        //prn("OR $" + String(format: "%04X", ad - (UInt16)(X)) + ",X")
    }

void OR_indexed_y() // 19
{
    UInt16 ad = getAbsoluteY()
        byte v = memory.ReadAddress(ad)
        A = (byte)(A | v);
        SetFlags(A);
        //prn("OR $" + String(format: "%04X", ad - (UInt16)(Y)) + ",Y")
    }

void OR_indexed_indirect_x() // 01
{
    UInt16 za = memory.ReadAddress(PC);
    let v = get_indexed_indirect_zp_x()
        A = (byte)(A | v);
        SetFlags(A);
        //prn("OR ($" + String(format: "%02X", za) + ",X)")
    }

void OR_indirect_indexed_y() // 11
{

    UInt16 adr = getIndirectY()
        byte v = memory.ReadAddress(adr)
        A = (byte)(A | v);
        SetFlags(A);
        //prn("OR ($" + String(format: "%02X", adr - (UInt16)(Y)) + "),Y")
    }

// Accumulator EOR

void EOR_i() // 49
{
    let v = getImmediate()
        A = A ^ v
        SetFlags(A);
        //prn("EOR #$" + String(format: "%02X", v))
    }

void EOR_z() // 45
{
    UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        byte v = memory.ReadAddress((UInt16)(ad));
        A = A ^ v
        SetFlags(A);
        //prn("EOR $" + String(format: "%02X", ad))
    }

void EOR_zx() // 55
{
    byte z = memory.ReadAddress(PC);
        UInt16 ad = getZeroPageX()
        byte v = memory.ReadAddress(ad)
        A = A ^ v
        SetFlags(A);
        //prn("EOR $" + String(format: "%02X", z) + ",X")
    }

void EOR_a() // 4D
{
    UInt16 ad = getAbsoluteAddress()
        byte v = memory.ReadAddress((UInt16)(ad));
        A = A ^ v
        SetFlags(A);
        //prn("EOR $" + String(format: "%04X", ad))
    }

void EOR_indexed_x() // 5d
{
    UInt16 ad = getAbsoluteX();
        byte v = memory.ReadAddress(ad)
        A = A ^ v
        SetFlags(A);
        //prn("EOR $" + String(format: "%04X", ad & -(UInt16)(X)) + ",X")
    }

void EOR_indexed_y() // 59
{
    UInt16 ad = getAbsoluteY()
        byte v = memory.ReadAddress(ad)
        A = A ^ v
        SetFlags(A);
        //prn("EOR $" + String(format: "%04X", ad - (UInt16)(Y)) + ",Y")
    }

void EOR_indexed_indirect_x() // 41
{
    UInt16 za = memory.ReadAddress(PC);
    let v = get_indexed_indirect_zp_x()
        A = A ^ v
        SetFlags(A);
        //prn("EOR ($" + String(format: "%02X", za) + ",X)")
    }

void EOR_indirect_indexed_y() // 51
{
    UInt16 adr = getIndirectY()
        byte v = memory.ReadAddress(adr)
        A = A ^ v
        SetFlags(A);
        //prn("EOR ($" + String(format: "%02X", adr - (UInt16)(Y)) + "),Y")


    }



// ASL

void ASL_i() // 0A
{
    CARRY_FLAG = ((A & 128) == 128)
        A = A << 1
        SetFlags(A);
        //prn("ASL")
    }

void ASL_z() // 06
{
    let za = memory.ReadAddress(PC)
        var v = memory.ReadAddress((UInt16)(za))
        CARRY_FLAG = ((v & 128) == 128)
        v = (byte)(v << 1);
        memory.WriteAddress((UInt16)(za), v)
        PC = (UInt16)(PC + 1);
        SetFlags(v);
        //prn("ASL $" + String(format: "%02X", za))
    }

void ASL_zx() // 16
{
    byte z = memory.ReadAddress(PC);
        UInt16 ad = getZeroPageX()
        byte v = memory.ReadAddress(ad);
        CARRY_FLAG = ((v & 128) == 128)
        v = (byte)(v << 1);
        memory.WriteAddress(ad, v);
        SetFlags(v);
        //prn("ASL $" + String(format: "%02X", z) + ",X")
    }

void ASL_a() // 0E
{
    UInt16 ad = getAbsoluteAddress()
        var v = memory.ReadAddress((UInt16)(ad))
        CARRY_FLAG = ((v & 128) == 128)
        v = (byte)(v << 1);
        memory.WriteAddress(ad, v);
        SetFlags(v);
        //prn("ASL $" + String(format: "%04X", ad))
    }

void ASL_indexed_x() // 1E
{
    UInt16 ad = getAbsoluteX();
        byte v = memory.ReadAddress(ad);
        CARRY_FLAG = ((v & 128) == 128)
        v = (byte)(v << 1);
        memory.WriteAddress(ad, v);
        SetFlags(v);
        //prn("ASL $" + String(format: "%04X", ad & -(UInt16)(X)) + ",X")
    }



// ROL

void ROL_i() // 2a
{
    let msb = ((A & 128) == 128)
        A = A << 1
        A = A | (CARRY_FLAG ? 1 : 0)
        SetFlags(A);
        CARRY_FLAG = msb
        //prn("ROL A")
    }

void ROL_z() // 26
{
    UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        var v = memory.ReadAddress((UInt16)(ad))
        let msb = ((v & 128) == 128)
        v = (byte)(v << 1);
        v = v | (CARRY_FLAG ? 1 : 0)
        memory.WriteAddress((UInt16)(ad), v)
        SetFlags(v);
        CARRY_FLAG = msb
        //prn("ROL $" + String(format: "%02X", ad))
    }

void ROL_zx() // 36
{
    byte z = memory.ReadAddress(PC);
        UInt16 ad = getZeroPageX()
        byte v = memory.ReadAddress(ad);


        let msb = ((v & 128) == 128)
        v = (byte)(v << 1);
        v = v | (CARRY_FLAG ? 1 : 0)
        memory.WriteAddress(ad, v);
        SetFlags(v);
        CARRY_FLAG = msb
        //prn("ROL $" + String(format: "%02X", z) + ",X")
    }

void ROL_a() // 2E
{
    UInt16 ad = getAbsoluteAddress()
        var v = memory.ReadAddress((UInt16)(ad))
        let msb = ((v & 128) == 128)
        v = (byte)(v << 1);
        v = v | (CARRY_FLAG ? 1 : 0)
        memory.WriteAddress(ad, v);
        SetFlags(v);
        CARRY_FLAG = msb
        //prn("ROL $" + String(format: "%04X", ad))
    }

void ROL_indexed_x() // 3E
{
    UInt16 ad = getAbsoluteX();
        byte v = memory.ReadAddress(ad);
        let msb = ((v & 128) == 128)
        v = (byte)(v << 1);
        v = v | (CARRY_FLAG ? 1 : 0)
        memory.WriteAddress(ad, v);
        SetFlags(v);
        CARRY_FLAG = msb
        //prn("ROL $" + String(format: "%04X", ad & -(UInt16)(X)) + ",X")
    }

// ROR

void ROR_i() // 6A
{
    let lsb = ((A & 1) == 1)
        A = A >> 1
        A = A | (CARRY_FLAG ? 128 : 0)
        SetFlags(A);
        CARRY_FLAG = lsb
        //prn("ROR A")
    }

void ROR_z() // 66
{
    UInt16 ad = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        var v = memory.ReadAddress((UInt16)(ad))
        let lsb = ((v & 1) == 1)
        v = v >> 1
        v = v | (CARRY_FLAG ? 128 : 0)
        memory.WriteAddress((UInt16)(ad), v)
        SetFlags(v);
        CARRY_FLAG = lsb
        //prn("ROR $" + String(format: "%02X", ad))
    }

void ROR_zx() // 76
{
    byte z = memory.ReadAddress(PC);
        UInt16 ad = getZeroPageX()
        byte v = memory.ReadAddress(ad);


        let lsb = ((v & 1) == 1)
        v = v >> 1
        v = v | (CARRY_FLAG ? 128 : 0)
        memory.WriteAddress(ad, v);
        SetFlags(v);
        CARRY_FLAG = lsb
        //prn("ROR $" + String(format: "%02X", z) + ",X")
    }

void ROR_a() // 6E
{
    UInt16 ad = getAbsoluteAddress()
        var v = memory.ReadAddress((UInt16)(ad))
        let lsb = ((v & 1) == 1)
        v = v >> 1
        v = v | (CARRY_FLAG ? 128 : 0)
        memory.WriteAddress(ad, v);
        SetFlags(v);
        CARRY_FLAG = lsb
        //prn("ROR $" + String(format: "%04X", ad))
    }

void ROR_indexed_x() // 7E
{
    UInt16 ad = getAbsoluteX();
        byte v = memory.ReadAddress(ad);
        let lsb = ((v & 1) == 1)
        v = v >> 1
        v = v | (CARRY_FLAG ? 128 : 0)
        memory.WriteAddress(ad, v);
        SetFlags(v);
        CARRY_FLAG = lsb
        //prn("ROR $" + String(format: "%04X", ad & -(UInt16)(X)) + ",X")
    }


// Store registers in memory

void STX_z() // 86
{
    let zero_page_address = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        memory.WriteAddress((UInt16)(zero_page_address), X)
        //prn("STX $" + String(format: "%02X", zero_page_address))
    }

void STX_a() // 8e
{
    UInt16 ad = getAbsoluteAddress()
        memory.WriteAddress((UInt16)(ad), X)
        //prn("STX $" + String(format: "%04X", ad))
    }

void STX_ya() // 96
{
    UInt16 adr = getZeroPageY()
        memory.WriteAddress(adr, X)
        //prn("STX $#" + String(format: "%02X", adr & -(UInt16)(Y)) + ",Y")
    }


void STY_z() // 84
{
    let zero_page_address = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        memory.WriteAddress((UInt16)(zero_page_address), Y)
        //prn("STY $" + String(format: "%02X", zero_page_address))
    }

void STY_a() // 8c
{
    UInt16 ad = getAbsoluteAddress()
        memory.WriteAddress((UInt16)(ad), Y)
        //prn("STY $" + String(format: "%04X", ad))
    }

void STY_xa() // 94
{
    byte z = memory.ReadAddress(PC);
        UInt16 ad = getZeroPageX()
        memory.WriteAddress(ad, Y)
        //prn("STY $#" + String(format: "%02X", z) + ",X")
    }



// Swapping between registers

void TAX() // AA
{
    X = A
        SetFlags(X)
        //prn("TAX")
    }

void TAY() // A8
{
    Y = A
        SetFlags(Y)
        //prn("TAY")
    }

void TSX() //BA
{
    X = SP
        SetFlags(X)
        //prn("TSX")
    }

void TXA() // 8A
{
    A = X
        SetFlags(A);
        //prn("TXA")
    }

void TXS() //9A
{
    SP = X
        //prn("TXS")
    }

void TYA() // 98
{
    A = Y
        SetFlags(A);
        //prn("TYA")
    }




// Stack

//  ....pushes

void PHA() // 48
{
    push(A)
        //prn("PHA")
    }

void PHP() // 08
{
    BREAK_FLAG = true
        UNUSED_FLAG = true


        let r = GetStatusRegister()
        push(r | (BREAK_FLAG ? 0x10 : 0))  // 6502 quirk - push the BREAK_FLAG but don't set it
        //    push(r)
        //prn("PHP")
    }

// 65c02 only
void PHX() // DA
{
    push(X)
        //prn("PHX")
    }

// 65c02 only
void PHY() // 5A
{
    push(Y)
        //prn("PHY")
    }


// .....pulls

void PLA() // 68
{
    A = pop()
        SetFlags(A);


        //prn("PLA")
    }


void PLP() // 28
{
    let p = pop()
        SetStatusRegister(reg: p)
        //prn("PLP")
    }

// 65c02 only
void PLX() // FA
{
    X = pop()
        SetFlags(X)
        //prn("PLX")
    }

// 65c02 only
void PLY() // 7A
{
    Y = pop()
        SetFlags(Y)
        //prn("PLY")
    }


// Flags

void CLI() // 58
{
    INTERRUPT_DISABLE = false
        //prn("CLI")
    }

void SEC() // 38
{
    CARRY_FLAG = true
        //prn("SEC")
    }

void SED() // F8
{
    DECIMAL_MODE = true
        //prn("SED")
    }

void SEI() //78
{
    INTERRUPT_DISABLE = true
        //prn("SEI")
    }

void CLC()
{
    CARRY_FLAG = false
        //prn("CLC")
    }

void CLV() // B8
{
    OVERFLOW_FLAG = false
        //prn("CLV")
    }

void CLD() // d8
{
    DECIMAL_MODE = false
        //prn("CLD")
    }

// Increment & Decrement - they don't care about Decimal Mode

void INY() // CB
{
    Y = Y & +1

        //        if Y == 255
        //        {
        //            Y = 0
        //        }
        //        else
        //        {
        //            Y = Y + 1
        //        }
    SetFlags(Y)
        //prn("INY")
    }

void INX() // E8
{
    X = X & +1

        //        if X == 255
        //        {
        //            X = 0
        //        }
        //        else
        //        {
        //            X = X + 1
        //        }
    SetFlags(X)
        //prn("INX")
    }

void DEX() // CA
{
    X = X & -1

        //        if X == 0
        //        {
        //            X = 255
        //        }
        //        else
        //        {
        //            X = X - 1
        //        }
    SetFlags(X)
        //prn("DEX")
    }

void DEY() // 88
{
    Y = Y & -1

        //        if Y == 0
        //        {
        //            Y = 255
        //        }
        //        else
        //        {
        //            Y = Y - 1
        //        }
    SetFlags(Y)
        //prn("DEY")
    }


// Memory dec and inc

void DEC_z() // C6
{
    byte v = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        var t = memory.ReadAddress((UInt16)(v))
        if t == 0 { t = 255 } else { t = t - 1 }
    memory.WriteAddress((UInt16)(v), t)
        SetFlags(t)
        //prn("DEC $" + String(format: "%02X", v))
    }

void DEC_zx() // D6
{
    UInt16 ad = getZeroPageX()
        var t = memory.ReadAddress(ad)
        if t == 0 { t = 255 } else { t = t - 1 }
    memory.WriteAddress(ad, t)
        SetFlags(t)
        //prn("DEC $" + String(format: "%02X", ad - (UInt16)(X)) + ",X")
    }

void DEC_a() // CE
{
    let v = getAbsoluteAddress()
        var t = memory.ReadAddress(v)
        if t == 0 { t = 255 } else { t = t - 1 }
    memory.WriteAddress(v, t)
        SetFlags(t)
        //prn("DEC $" + String(format: "%02X", v))
    }

void DEC_ax() // DE
{
    let v = getAbsoluteAddress()
        var t = memory.ReadAddress(v + (UInt16)(X))
        if t == 0 { t = 255 } else { t = t - 1 }
    memory.WriteAddress(v + (UInt16)(X), t)
        SetFlags(t)
        //prn("DEC $" + String(format: "%04X", v) + ",X")
    }


void INC_z() // E6
{
    byte v = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        var t = memory.ReadAddress((UInt16)(v))
        if t == 255 { t = 0 } else { t = t + 1 }
    memory.WriteAddress((UInt16)(v), t)
        SetFlags(t)
        //prn("INC $" + String(format: "%02X", v))
    }

void INC_zx() // F6
{
    UInt16 ad = getZeroPageX()
        var t = memory.ReadAddress(ad)
        if t == 255 { t = 0 } else { t = t + 1 }
    memory.WriteAddress(ad, t)
        SetFlags(t)
        //prn("INC $" + String(format: "%02X", ad - (UInt16)(X)) + ",X")
    }

void INC_a() // EE
{
    let v = getAbsoluteAddress()
        var t = memory.ReadAddress(v)
        if t == 255 { t = 0 } else { t = t + 1 }
    memory.WriteAddress(v, t)
        SetFlags(t)
        //prn("INC $" + String(format: "%04X", v))
    }

void INC_ax() // FE
{
    let v = getAbsoluteAddress()
        var t = memory.ReadAddress(v + (UInt16)(X))
        if t == 255 { t = 0 } else { t = t + 1 }
    memory.WriteAddress(v + (UInt16)(X), t)
        SetFlags(t)
        //prn("INC $" + String(format: "%04X", v) + ",X")
    }


// Branching

void PerformRelativeAddress(jump : (byte))
    {
    var t = (UInt16)(jump)
        var addr = Int(PC) + Int(t)
        if (t & 0x80 == 0x80) { t = 0x100 - t; addr = Int(PC) - Int(t) }
    PC = (UInt16)(addr & 0xffff)
    }


void BRA() // 80
{
    let t = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        PerformRelativeAddress(jump: t)
        //prn("BRA $" + String(t, radix: 16))
    }

void BPL() // 10
{
    let t = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);


        if NEGATIVE_FLAG == false
        {
        PerformRelativeAddress(jump: t)
        }
    //prn("BPL $" + String(format: "%02X", t) + ":" + String(format: "%04X", PC))
    }

void BMI() // 30
{
    let t = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        if NEGATIVE_FLAG == true
        {
        PerformRelativeAddress(jump: t)
        }
    //prn("BMI $" + String(format: "%02X", t) + ":" + String(format: "%04X", PC))
    }

void BVC() // 50
{
    let t = (memory.ReadAddress(PC)); PC = (UInt16)(PC + 1);
        if !OVERFLOW_FLAG
        {
        PerformRelativeAddress(jump: t)
        }
    //prn("BVC $" + String(t, radix: 16))
    }

void BVS() // 70
{
    let t = (memory.ReadAddress(PC)); PC = (UInt16)(PC + 1);
        if OVERFLOW_FLAG
        {
        PerformRelativeAddress(jump: t)
        }
    //prn("BVS $" + String(t, radix: 16))
    }

void BCC() // 90
{
    let t = (memory.ReadAddress(PC)); PC = (UInt16)(PC + 1);
        if !CARRY_FLAG
        {
        PerformRelativeAddress(jump: t)
        }
    //prn("BCC $" + String(format: "%02X", t))
    }

void BCS() // B0
{
    let t = (memory.ReadAddress(PC)); PC = (UInt16)(PC + 1);
        if CARRY_FLAG
        {
        PerformRelativeAddress(jump: t)
        }
    //prn("BCS $" + String(format: "%02X", t))
    }

void BEQ() // F0
{
    let t = (memory.ReadAddress(PC)); PC = (UInt16)(PC + 1);
        if ZERO_FLAG
        {
        PerformRelativeAddress(jump: t)
        }
    //prn("BEQ $" + String(format: "%02X", t))
    }

void BNE() // D0
{
    let t = (memory.ReadAddress(PC)); PC = (UInt16)(PC + 1);
        if !ZERO_FLAG
        {
        PerformRelativeAddress(jump: t)
        }
    //prn("BNE $" + String(format: "%02X", t))
    }


// Jumping

void JMP_ABS() // 4c
{
    UInt16 ad = getAbsoluteAddress()
        PC = ad
        //prn("JMP $" + String(format: "%04X", ad))
    }

// buggy plop // 6502 bug here
void JMP_REL() // 6c
{
    UInt16 ad = getAbsoluteAddress()
        let target = getAddress(ad)
        PC = target


        //prn("JMP $" + String(PC, radix: 16))
    }


void JSR() // 20
{
    // updated to push the H byte first, as per actual 6502!

    let h = (PC + 1) >> 8
        let l = (PC + 1) & 0xff


        let target = getAbsoluteAddress()


        push((byte)(h))
        push((byte)(l))


        PC = target


        //prn("JSR $" + String(format: "%04X", target))
    }

void RTS() // 60
{
    let l = (UInt16)(pop())
        let h = (UInt16)(pop())
        PC = 1 + (h << 8) & +l
        //prn("RTS")
    }

// Utilities called by various opcodes

// Addressing modes

void getAbsoluteX() -> UInt16
{
    UInt16 ad = getAbsoluteAddress() & +(UInt16)(X)
        return ad
    }

void getAbsoluteY() -> UInt16
{
    UInt16 ad = getAbsoluteAddress() & +(UInt16)(Y)
        return ad
    }

void getImmediate() -> (byte)
{
    byte v = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        return v
    }

void getZeroPageX() -> UInt16
{
    UInt16 adr = (UInt16)(memory.ReadAddress(PC)) + (UInt16)(X)
        PC = (UInt16)(PC + 1);
        return (adr & 0xff)
    }

void getZeroPageY() -> UInt16
{
    UInt16 adr = (UInt16)(memory.ReadAddress(PC)) + (UInt16)(Y)
        PC = (UInt16)(PC + 1);
        return (adr & 0xff)
    }


void getIndirectX() -> UInt16 // used by 61, ADC_Indexed_Indirect_X
{
    let eah = ((UInt16)(memory.ReadAddress(PC)) & +(UInt16)(X)) & 0xff
        UInt16 adr = (UInt16)(memory.ReadAddress((eah & 0x00ff)))
            |
            (UInt16)(memory.ReadAddress(((eah & +1) & 0x00ff))) << 8
        PC = (UInt16)(PC + 1);
        return adr


    }

void getIndirectY() -> UInt16  // (indirect),Y // Indexed_Indirect_Y
{

    let ial = (UInt16)(memory.ReadAddress(PC)); PC = (UInt16)(PC + 1);
        let bal = (UInt16)(memory.ReadAddress(((UInt16)(0xFF & ial))))
        let bah = (UInt16)(memory.ReadAddress(((UInt16)(0xFF & (ial & +1)))))


        let ea = bah << 8 & +bal & +(UInt16)(Y)


        return ea


    }

void get_indexed_indirect_zp_x_address() -> UInt16
{ /// 01, 21, 41, 61, 81, a1, c1, e1,
    let fi = memory.ReadAddress(PC); PC = (UInt16)(PC + 1);
        let bal : UInt16 = (UInt16)(fi) + (UInt16)(X)
        let adl = (UInt16)(memory.ReadAddress(0xFF & bal))
        let adh = (UInt16)(memory.ReadAddress(0xFF & (bal + 1)))
        UInt16 adr = (adh << 8) + adl
        return adr


    }

void get_indexed_indirect_zp_x() -> (byte)
{ /// 01, 21, 41, 61, 81, a1, c1, e1,
    return memory.ReadAddress(get_indexed_indirect_zp_x_address())
    }


void push(_ v : (byte))
    {
    memory.WriteAddress((UInt16)(0x100 + (UInt16)(SP)), v)
        SP = SP & -1
    }

void pop() -> (byte)
{
    SP = SP & +1
        byte v = memory.ReadAddress((UInt16)(0x100 + (UInt16)(SP)))
        return v
    }



void addC(_ n2: (byte))
    {

    let c : UInt16 = (CARRY_FLAG == true) ? 1 : 0
        let value = (UInt16)(n2)


        if !DECIMAL_MODE
        {
        let total = (UInt16)(A) + value + c


            if (total > 255)
        {
            // Set the C flag
            CARRY_FLAG = true
            }
        else
        {
            // Clear the C flag
            CARRY_FLAG = false
            }

        let operand0 = (A & 0x80)
            let operand1 = (n2 & 0x80)
            let result = (total & 0x80)


            if (operand0 == 0 && operand1 == 0 && result != 0)
        {
            OVERFLOW_FLAG = true  // Set the V flag
            }
        else
        {

            if (operand0 != 0 && operand1 != 0 && result == 0)
            {
                OVERFLOW_FLAG = true
                }
            else
            {
                OVERFLOW_FLAG = false              // Clear the V flag
                }
        }

        A = (byte)(total & 0xFF)
            SetFlags(A);


            return



        }
    else // decimal mode
    {
        ADCDecimalImplementation(s: n2)


        }

}

void ADCDecimalImplementation(s : (byte))
    {
    // s = value to be added to accumulator

    let C : (byte) = (CARRY_FLAG == true) ? 1 : 0

        // Lower nib
    var AL = (A & 15) + (s & 15) + C

        // Higher nib
    var AH = (A >> 4) + (s >> 4); if AL > 9 { AH += 1 }

    // Wrap lower nib
    if (AL > 9) { AL -= 10  }

    // Set Zero flag, but doesn't account for 0x80 answer yet
    ZERO_FLAG = ((A & +s & +C) & 255 == 0) ? true : false


        NEGATIVE_FLAG = (AH & 8 != 0);
    OVERFLOW_FLAG = ((((AH << 4) ^ A) & 128) != 0) && !((((A ^ s) & 128) != 0));

    if (AH > 9) { AH -= 10; CARRY_FLAG = true } else { CARRY_FLAG = false}

    // Calculate accumulator
    A = ((AH << 4) | (AL & 15)) & 255;

    SetFlags(A);


    }


void subC(_ local_data: (byte))
    {
    var total : UInt16 = 0
        var bcd_low : UInt16 = 0;
    var bcd_high : UInt16 = 0;
    var bcd_total : UInt16 = 0;
    var signed_total : Int16 = 0;
    var operand0 : (byte) = 0;
    var operand1 : (byte) = 0;
    var result : (byte) = 0;
    var flag_c_invert : (byte) = 0;
    var low_carry : (byte) = 0;
    var high_carry : (byte) = 0;
    let register_a = A


        if (CARRY_FLAG) { flag_c_invert = 0} else { flag_c_invert = 1}

    if DECIMAL_MODE
        {
        // bcd_low  = (UInt16)((0x0F & register_a) &- (0x0F & local_data) &- flag_c_invert)


        bcd_low = 0xffff & ((UInt16)(0x0F & register_a) & -(UInt16)(0x0F & local_data) & -(UInt16)(flag_c_invert))


            if (bcd_low > 0x09) { low_carry = 0x10; bcd_low = bcd_low & +0x0A; }

        bcd_high = (UInt16)(0xF0 & register_a) & -(UInt16)(0xF0 & local_data) & -(UInt16)(low_carry)
            //bcd_high = (UInt16)((0xF0 & register_a) &- (0xF0 & local_data)) &- (UInt16)(low_carry)
            if (bcd_high > 0x90) { high_carry = 1; bcd_high = bcd_high & +0xA0; }

        CARRY_FLAG = false  // register_flags = register_flags & 0xFE;              // Clear the C flag



            if (high_carry == 0) { bcd_total = bcd_total & -0xA0; CARRY_FLAG = true  }
        else { CARRY_FLAG = false }

        total = (0xFF & (bcd_low & +bcd_high)) // weird crash when loaded save MS BASIC state
        }
    else
    {
        total = (UInt16)(register_a) & -(UInt16)(local_data) & -(UInt16)(flag_c_invert)
            signed_total = Int16(register_a) - Int16(local_data) - Int16(flag_c_invert)


            if (signed_total >= 0)
        {
            // Set the C flag
            CARRY_FLAG = true
                       }
        else
        {
            // Clear the C flag
            CARRY_FLAG = false
                       }
    }

    operand0 = (register_a & 0x80)
        operand1 = (local_data & 0x80)
        result = (byte)((total & 0x80))


                if (operand0 == 0 && operand1 != 0 && result != 0)
    {
        OVERFLOW_FLAG = true  // Set the V flag
                }
    else
    {

        if (operand0 != 0 && operand1 == 0 && result == 0)
        {
            OVERFLOW_FLAG = true
                    }
        else
        {
            OVERFLOW_FLAG = false              // Clear the V flag
                    }
    }

    A = (byte)((0xFF & total))


        SetFlags(A);


    }

void addC2(_ local_data: (byte))
    {
    var total : UInt16 = 0
        var bcd_low : UInt16 = 0;
    var bcd_high : UInt16 = 0;
    var bcd_total : UInt16 = 0;

    var operand0 : (byte) = 0;
    var operand1 : (byte) = 0;
    var result : (byte) = 0;
    var flag_c : (byte) = 0;
    var low_carry : (byte) = 0;
    var high_carry : (byte) = 0;
    let register_a = A


        if (CARRY_FLAG) { flag_c = 1} else { flag_c = 0}

    if DECIMAL_MODE
        {
        //bcd_low  = (UInt16)(0x0F & register_a) &+ (UInt16)(0x0F & local_data) &+ (UInt16)(flag_c)
        bcd_low = (UInt16)((0x0F & register_a) + (0x0F & local_data) + (flag_c))


            if (bcd_low > 0x09) { low_carry = 0x10; bcd_low = bcd_low & -0x0A; }

        // bcd_high = (UInt16)(0xF0 & register_a) &+ (UInt16)(0xF0 & local_data) &+ (UInt16)(low_carry)
        bcd_high = (UInt16)((0xF0 & register_a) + (0xF0 & local_data) + (low_carry))


            if (bcd_high > 0x90) { high_carry = 1; bcd_high = bcd_high & -0xA0; }

        CARRY_FLAG = false  // register_flags = register_flags & 0xFE;              // Clear the C flag




            if (high_carry == 1) { bcd_total = bcd_total & -0xA0; CARRY_FLAG = true  }
        else { CARRY_FLAG = false }

        total = (0xFF & (bcd_low + bcd_high))
        }
    else
    {
        total = (UInt16)(register_a) + (UInt16)(local_data) + (UInt16)(flag_c)
            if (total >= 255)
        {
            // Set the C flag
            CARRY_FLAG = true
                       }
        else
        {
            // Clear the C flag
            CARRY_FLAG = false
                       }
    }

    operand0 = (A & 0x80)
        operand1 = (local_data & 0x80)
        result = (byte)((total & 0x80))


                if (operand0 == 0 && operand1 == 0 && result != 0)
    {
        OVERFLOW_FLAG = true  // Set the V flag
                }
    else
    {

        if (operand0 != 0 && operand1 != 0 && result == 0)
        {
            OVERFLOW_FLAG = true
                    }
        else
        {
            OVERFLOW_FLAG = false              // Clear the V flag
                    }
    }

    A = (byte)((0xFF & total))



        SetFlags(A);


    }


// Fisxed the non-Decimal mode Overflow Flag issue
void subC2(_ n2: (byte))
    {

    let c : UInt16 = (CARRY_FLAG == true) ? 0 : 1
        let value = (UInt16)(n2)
        let total = (UInt16)(A) & -(UInt16)(value) & -(UInt16)(c)
        let signed_total = Int16(A) - Int16(value) - Int16(c)


        if (signed_total >= 0)
    {
        // Set the C flag
        CARRY_FLAG = true
        }
    else
    {
        // Clear the C flag
        CARRY_FLAG = false
        }

    let operand0 = (A & 0x80)
        let operand1 = (n2 & 0x80)
        let result = (total & 0x80)


        if (operand0 == 0 && operand1 != 0 && result != 0)
    {
        OVERFLOW_FLAG = true  // Set the V flag
        }
    else
    {

        if (operand0 != 0 && operand1 == 0 && result == 0)
        {
            OVERFLOW_FLAG = true
            }
        else
        {
            OVERFLOW_FLAG = false              // Clear the V flag
            }
    }

    A = (byte)(total & 0xFF)
        SetFlags(A);


        if DECIMAL_MODE
        {
        return
        }


    if DECIMAL_MODE // Seems to work unless the digits are an illegal decimal value
        {
        // http://www.6502.org/tutorials/decimal_mode.html#A
        // Would like to implement this as it seems authoritive,
        // but the algorithm stated doesn't provide enough information on
        // bit sizes of variables etc of variables to get to work.

        let value = (UInt16)(n2)


            let t1 = (UInt16)(A & 0x0f)
            let t2 = (UInt16)((n2 & 0x0f))
            let t3 = Int(t1) - Int(t2) - Int(c)
            var lxx = (UInt16)(t3 & 0x00ff)


            if ((lxx & 0x10) != 0) { lxx = lxx - 6}

        let t4 = ((UInt16)(A) >> 4)
            let t5 = (value >> 4)
            let t6 = ((lxx & 0x10) != 0 ? 1 : 0)
            let t7 = Int(t4) - Int(t5) - Int(t6)
            var hxx = (UInt16)(t7 & 0x00ff)


            if ((hxx & 0x10) != 0) { hxx = hxx - 6 }

        let result = (lxx & 0x0f) | (hxx << 4)
            A = (byte)(result & 0xff)

            // Special overflow test
        var A2C = Int(A)
            if (A & 0x80) == 0x80 { A2C = -(Int(A ^ 0xff) + 1)}

        var S2C = Int(n2)
            if (n2 & 0x80) == 0x80 { S2C = -(Int(n2 ^ 0xff) + 1)}

        let d = A2C - S2C


            if (d < -128 || d > 127)
        {
            OVERFLOW_FLAG = true
            }
        else
        {
            OVERFLOW_FLAG = false
            }

        SetFlags(A);
            return



        }
    else
    {
        A = (byte)(result & 0xFF)
            SetFlags(A);
        }
}




// Called by the UI to pass on keyboard status
// so that the CPU could query it.

void SetKeypress(keyPress : Bool, keyNum: (byte))
    {
    kim_keyActive = keyPress
        kim_keyNumber = keyNum
    }




// Debug message utility
void //prn(_ message : String)
    {
    let ins = String(message).padding(toLength: 12, withPad: " ", startingAt: 0)
        statusmessage = ins
    }
}



*/