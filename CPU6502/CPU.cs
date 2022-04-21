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
            return memory.ReadAddress(address: (UInt16)(0x100 + (UInt16)(SP)));

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


bool ProcessInstruction(byte instruction )
{

    switch (instruction) {

        case 0:
                    BRK(); break;
        case 1:
                    OR_indexed_indirect_x(); break;


        case 5:
                    OR_z();
        case 06:
                    ASL_z();


        case 8:
            PHP()
        case 9:
            OR_i();
        case 0x0A:
            ASL_i();


        case 0x0D:
            OR_a();
        case 0x0E:
            ASL_a();


        case 0x10:
            BPL();
        case 0x11:
            OR_indirect_indexed_y();


        case 0x15:
            OR_zx();
        case 0x16:
            ASL_zx();
        case 0x18:
            CLC();
        case 0x19:
            OR_indexed_y();


        case 0x1D:
            OR_indexed_x();
        case 0x1E:
            ASL_indexed_x();


        case 0x20:
            JSR();
        case 0x21:
            AND_indexed_indirect_x();


        case 0x24:
            BIT_z();
        case 0x25:
            AND_z();
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
            return false


        }

    return true
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

   
  
    
   
    
    func SetTTYMode(TTY : Bool)
    {
        // These memory addresses will have different values depending on the KIM working in LED mode or Serial terminal mode.
        // They're effectively hardware settings made by a switch on the board.
       
        if TTY // Console mode
        {
            Write(address: 0x1740, byte: 0x00)
            Write(address: 0x00ff, byte: 0x00)
        }
        else // HEX keypad and LEDs
        {
            Write(address: 0x1740, byte: 0xFF)
            Write(address: 0x00ff, byte: 0x01)
        }
    }
    
    // When saving and loading, it's important to save the Registers and PC state.
    func GetStatus() -> [(byte)]
    {
        let pch = (byte)(GetPC() >> 8)
        let pcl = (byte)(GetPC() & 0x00ff)
        return [A, X, Y, SP, GetStatusRegister(), pcl, pch]
    }
    func SetStatus(flags : [(byte)])
    {
        A = flags[0]
        X = flags[1]
        Y = flags[2]
        SP = flags[3]
        SetStatusRegister(reg: flags[4])
        SetPC(ProgramCounter: UInt16(flags[6]) << 8 + UInt16(flags[5]))
    }
    
    func LoadFromDocuments() -> Bool // This load routine also loads in register state (so it's > 64Kb by 7 bytes)
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
    
    func LoadFromBundle() -> Bool
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
    
    func SaveToDocuments()
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
    
    
    func AppleActive( state : Bool)
    {
        APPLE_ACTIVE = state
        memory.AppleActive(state: APPLE_ACTIVE)
    }
    
    func AppleReady() -> Bool
    {
        return memory.AppleReady()
    }
    
    func AppleKeyboard(s : Bool, k : (byte))
    {
        memory.AppleKeyState(state: s, key: k)
    }
    
    func AppleOutput() ->(Bool, (byte))
    {
        return memory.getAppleOutputState()
    }
    
    
    func APPLE_ROM_Code (address: UInt16)
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
    
    func KIM_ROM_Code (address: UInt16)
    {
        // This is the KIM specfic part.
        
        // Detect when these routines are being called or jmp'd to and then
        // perform the action and skip to their exit point.
        
        switch (PC) {
                 
        case 0x1F1F :
            prn("SCANDS"); // Also sets Z to 0 if a key is being pressed
           
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
            self.prn("DETCPS")
            memory.WriteAddress(address: 0x17F3, value: 1)
            memory.WriteAddress(address: 0x17F2, value: 1)
            PC = 0x1C4F
            
        case 0x1EFE : // The AK call is a "is someone pressing my keyboard?"
            self.prn("AK")
            
            if memory.ReadAddress(address: 0xff) == 0 // LED mode
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
            self.prn("GETKEY \(kim_keyNumber)")
          
            if kim_keyActive
            {
                A = kim_keyNumber
                SetFlags(value: A)
            }
            else
            {
                A = 0xFF
                SetFlags(value: A)
            }
            
            kim_keyNumber = 0
            kim_keyActive = false
    
            
            PC = 0x1F90
            
        case 0x1EA0 : // intercept OUTCH (send char to serial) and display on "console"
            self.prn("OUTCH")
            
            if A >= 13
            {
                texttodisplay.append(String(format: "%c", A))
            }
            Y = 0xFF
            A = 0xFF
            PC = 0x1ED3
            
            
        case 0x1E65 : //   //intercept GETCH (get char from serial). used to be 0x1E5A, but intercept *within* routine just before get1 test
            self.prn("GETCH")
            
           

A = GetAKey()


            if (A == 0)
{
    PC = 0x1E60;    // cycle through GET1 loop for character start, let the 6502 runs through this loop in a fake way
    break
            }

X = memory.ReadAddress(address: 0xFD) // x is saved in TMPX by getch routine, we need to get it back in x;
            Y = 0xFF
            PC = 0x1E87


        default : break
            
        }
    }
    
    
    func Init(ProgramName : String, computer: String)
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

func Step() -> (address: UInt16, Break: Bool, opcode: String, display: Bool)
    {

    memory.RIOT_Timer_Click()


        dataToDisplay = false

        // Intercept some KIM-1 Specific things
    KIM_ROM_Code(address: PC)

        // Execute the instruction at PC
    if !Execute()
        {
        PC = 0xffff
        }
    RUNTIME_DEBUG_MESSAGES = true

        //print(memory.ReadAddress(address: 0x1740))
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
func StepSerial() -> (address: UInt16, Break: Bool, terminalOutput: String)
    {

    if !APPLE_ACTIVE
        {
        // Intercept some KIM-1 Specific things
        memory.RIOT_Timer_Click()
            KIM_ROM_Code(address: PC)
        }

    // Execute the instruction at PC
    _ = Execute()


        if APPLE_ACTIVE
        {
        APPLE_ROM_Code(address: PC)



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




func GetAKey() -> (byte)
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



func Dump(opcode : (byte))
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

func DisplayDebugInformation()
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

    let regs = String("\(String(format: " % 04X",Read(address: PC)))  PC:\(String(format: " % 04X", PC)) A:\(String(format: " % 02X",A)) X:\(String(format: " % 02X",X)) Y:\(String(format: " % 02X",Y)) SP:\(String(format: " % 02X",SP))  AC:\(String(format: " % 02X",memory.ReadAddress(address: 0x200)))  AC:\(String(format: " % 02X",memory.ReadAddress(address: 0x1A)))  AC:\(String(format: " % 02X",memory.ReadAddress(address: 0x19)))")


        printStatusToDebugWindow(regs, flags)


    }









// Implement the 6502 instruction set
//
// Addressing modes - http://www.obelisk.me.uk/6502/addressing.html
//



func RTI()
{
    // Used in the KIM-1 to launch user app

    SetStatusRegister(reg: pop())


        let l = UInt16(pop())
        let h = UInt16(pop())
        PC = (h << 8) + l
        prn("RTI")
    }

func NOP() // EA
{
    prn("NOP")
    }

// Accumulator BIT test - needs proper testing


func BIT_z() // 24
{
    let ad = memory.ReadAddress(address: PC); PC = PC + 1
        let v = memory.ReadAddress(address: UInt16(ad))
        let t = (A & v)
        ZERO_FLAG = (t == 0) ? true : false
        NEGATIVE_FLAG = (v & 128) == 128
        OVERFLOW_FLAG = (v & 64) == 64


        prn("BIT $" + String(format: "%02X", ad))
    }


func BIT_a() // 2C
{
    let ad = getAbsoluteAddress()
        let v = memory.ReadAddress(address: UInt16(ad))
        let t = (A & v)
        ZERO_FLAG = (t == 0) ? true : false
        NEGATIVE_FLAG = (v & 128) == 128
        OVERFLOW_FLAG = (v & 64) == 64


        prn("BIT $" + String(format: "%04X", ad))
    }

// Accumulator Addition

func ADC_i() // 69
{
    let v = memory.ReadAddress(address: PC); PC = PC + 1
        addC(v)
        prn("ADC #$" + String(format: "%02X", v))
    }

func ADC_indexed_indirect_x() // 61
{
    let za = memory.ReadAddress(address: PC);
    let v = get_indexed_indirect_zp_x()
        addC(v)
        prn("ADC ($" + String(format: "%02X", za) + ",X)")
    }

func ADC_z() // 65
{
    let ad = memory.ReadAddress(address: PC); PC = PC + 1
        let v = memory.ReadAddress(address: UInt16(ad))
        addC(v)
        prn("ADC $" + String(format: "%04X", v))
    }

func ADC_zx() // 75
{
    let zp = getZeroPageX()
        let v = memory.ReadAddress(address: zp)
        addC(v)
        prn("ADC $" + String(format: "%02X", zp & -UInt16(X)) + ",X")
    }

func ADC_a() // 6D
{
    let ad = getAbsoluteAddress()
        let v = memory.ReadAddress(address: UInt16(ad))
        addC(v)
        prn("ADC $" + String(format: "%04X", ad))
    }

func ADC_indexed_x() // 7d
{
    let ad = getAbsoluteX()
        let v = memory.ReadAddress(address: ad)
        addC(v)
        prn("ADC $" + String(format: "%04X", ad & -UInt16(X)))
    }

func ADC_indexed_y() // 79
{
    let ad = getAbsoluteY()
        let v = memory.ReadAddress(address: ad)
        addC(v)
        prn("ADC $" + String(format: "%04X", ad & -UInt16(Y)))
    }


func ADC_indirect_indexed_y() // 71
{
    let adr = getIndirectY()
        let v = memory.ReadAddress(address: adr)
        addC(v)
        prn("ADC ($" + String(format: "%02X", adr - UInt16(Y)) + "),Y")
    }




// Accumulator Subtraction

func SBC_i() // E9
{
    let v = memory.ReadAddress(address: PC); PC = PC + 1
        subC(v)
        prn("SBC #$" + String(format: "%02X", v))
    }

func SBC_z() // E5
{
    let zero_page_address = memory.ReadAddress(address: PC); PC = PC + 1
        let v = memory.ReadAddress(address: UInt16(zero_page_address))
        subC(v)
        prn("SBC $" + String(String(format: "%02X", zero_page_address)))
    }

func SBC_zx() // F5
{
    let ad = getZeroPageX()
        let v = memory.ReadAddress(address: ad)
        subC(v)
        prn("SBC $" + String(format: "%02X", ad & -UInt16(X)) + ",X")
    }

func SBC_a() // ed
{
    let ad = getAbsoluteAddress()
        let v = memory.ReadAddress(address: UInt16(ad))
        subC(v)
        prn("SBC $" + String(format: "%04X", ad))
    }

func SBC_indexed_x() // fd
{
    let ad = getAbsoluteX()
        let v = memory.ReadAddress(address: ad)
        subC(v)
        prn("SBC $" + String(format: "%04X", ad - UInt16(X)) + ",X")
    }

func SBC_indexed_y() // F9
{
    let ad = getAbsoluteY()
        let v = memory.ReadAddress(address: ad)
        subC(v)
        prn("SBC $" + String(format: "%04X", ad - UInt16(Y)) + ",Y")
    }

func SBC_indirect_indexed_y() // F1
{
    let adr = getIndirectY()
        let v = memory.ReadAddress(address: adr)
        subC(v)
        prn("SBC ($" + String(format: "%04X", adr - UInt16(Y)) + "),Y")
    }

func SBC_indexed_indirect_x() // E1
{
    let za = memory.ReadAddress(address: PC);
    let v = get_indexed_indirect_zp_x()
        subC(v)
        prn("SBC ($" + String(format: "%04X", za) + ",X)")
    }

// General comparision

func compare(_ n : (byte), _ v: (byte))
    {
    let result = Int16(n) - Int16(v)
        if n >= (byte)(v & 0xFF) { CARRY_FLAG = true } else { CARRY_FLAG = false }
    if n == (byte)(v & 0xFF) { ZERO_FLAG = true } else { ZERO_FLAG = false }
    if (result & 0x80) == 0x80 { NEGATIVE_FLAG = true } else { NEGATIVE_FLAG = false }
}

// X Comparisons

func CPX_i() // E0
{
    let v = memory.ReadAddress(address: PC); PC = PC + 1
        compare(X, v)
        prn("CPX #$" + String(format: "%02X", v))
    }

func CPX_z() // E4
{
    let ad = memory.ReadAddress(address: PC); PC = PC + 1
        let v = memory.ReadAddress(address: UInt16(ad))
        compare(X, v)
        prn("CPX $" + String(format: "%02X", ad))
    }

func CPX_A() // EC
{
    let ad = getAbsoluteAddress()
        let v = memory.ReadAddress(address: UInt16(ad))
        compare(X, v)
        prn("CPX $" + String(format: "%04X", ad))


    }

// Y Comparisons

func CPY_i() // C0
{
    let v = memory.ReadAddress(address: PC); PC = PC + 1
        compare(Y, v)
        prn("CPY #$" + String(format: "%02X", v))
    }

func CPY_z() // C4
{
    let ad = memory.ReadAddress(address: PC); PC = PC + 1
        let v = memory.ReadAddress(address: UInt16(ad))
        compare(Y, v)
        prn("CPY $" + String(format: "%02X", ad))
    }

func CPY_A()
{
    let ad = getAbsoluteAddress()
        let v = memory.ReadAddress(address: UInt16(ad))
        compare(Y, v)
        prn("CPY $" + String(format: "%04X", ad))


    }

// Accumulator Comparison

func CMP_i() // C9
{
    let v = memory.ReadAddress(address: PC); PC = PC + 1
        compare(A, v)
        prn("CMP #$" + String(format: "%02X", v))
    }

func CMP_z() // C5
{
    let ad = memory.ReadAddress(address: PC); PC = PC + 1
        let v = memory.ReadAddress(address: UInt16(ad))
        compare(A, v)
        prn("CMP $" + String(format: "%02X", ad))
    }

func CMP_zx() // D5
{
    let ad = getZeroPageX()
        let v = memory.ReadAddress(address: ad)
        compare(A, v)
        prn("CMP $" + String(format: "%02X", ad - UInt16(X)) + ",X")
    }

func CMP_a() // cd
{
    let ad = getAbsoluteAddress()
        let v = memory.ReadAddress(address: UInt16(ad))
        compare(A, v)
        prn("CMP $" + String(format: "%04X", ad))
    }

func CMP_indexed_x() // dd
{
    let ad = getAbsoluteX()
        let v = memory.ReadAddress(address: ad)
        compare(A, v)
        prn("CMP $" + String(format: "%04X", ad & -UInt16(X)) + ",X")
    }

func CMP_indexed_y() // d9
{
    let ad = getAbsoluteY()
        let v = memory.ReadAddress(address: ad)
        compare(A, v)
        prn("CMP $" + String(format: "%04X", ad - UInt16(Y)) + ",Y")
    }

func CMP_indirect_indexed_y() // D1
{
    let adr = getIndirectY()
        let v = memory.ReadAddress(address: adr)

        //let zp = UInt16(memory.ReadAddress(address: PC));
        //let v = memory.ReadAddress(address: getIndirectIndexedBase())
    compare(A, v)
        prn("CMP ($" + String(format: "%02X", adr - UInt16(Y)) + "),Y")
    }

func CMP_indexed_indirect_x() // c1
{
    let za = memory.ReadAddress(address: PC);
    let v = get_indexed_indirect_zp_x()
        compare(A, v)
        prn("CMP ($" + String(format: "%02X", za) + "),X")
    }


// Accumulator Loading

func LDA_i() // A9
{
    A = getImmediate()
        SetFlags(value: A)
        prn("LDA #$" + String(format: "%02X", A))
    }

func LDA_z() // A5
{
    let zero_page_ad = memory.ReadAddress(address: PC); PC = PC + 1
        A = memory.ReadAddress(address: UInt16(zero_page_ad))
        SetFlags(value: A)
        prn("LDA $" + String(format: "%02X", zero_page_ad))
    }

func LDA_zx() // B5
{
    let ad = getZeroPageX()
        A = memory.ReadAddress(address: ad)
        SetFlags(value: A)
        prn("LDA $" + String(format: "%02X", ad & -UInt16(X)) + ",X")
    }

func LDA_a() // ad
{
    let ad = getAbsoluteAddress()
        A = memory.ReadAddress(address: ad)
        SetFlags(value: A)
        prn("LDA $" + String(format: "%04X", ad))
    }

func LDA_indexed_x() // bd
{
    let ad = getAbsoluteX()
        A = memory.ReadAddress(address: ad)
        SetFlags(value: A)
        prn("LDA $" + String(format: "%04X", ad & -UInt16(X)) + ",X")
    }

func LDA_indexed_y() // B9
{
    let ad = getAbsoluteY()
        A = memory.ReadAddress(address: ad)
        SetFlags(value: A)
        prn("LDA $" + String(format: "%04X", ad & -UInt16(Y)) + ",Y")
    }

func LDA_indexed_indirect_x() // A1
{
    let za = memory.ReadAddress(address: PC);
    A = get_indexed_indirect_zp_x()
        SetFlags(value: A)
        prn("LDA ($" + String(format: "%02X", za) + ",X)")
    }

func LDA_indirect_indexed_y() // B1
{
    let adr = getIndirectY()
        A = memory.ReadAddress(address: adr)
        //let za = memory.ReadAddress(address: PC)
        // A = memory.ReadAddress(address: getIndirectIndexedBase())
    SetFlags(value: A)
        prn("LDA ($" + String(format: "%02X", adr - UInt16(Y)) + "),Y")
    }



// Accumulator Storing

func STA_z() // 85
{
    let zero_page_add = memory.ReadAddress(address: PC); PC = PC + 1
        memory.WriteAddress(address: UInt16(zero_page_add), value: A)
        prn("STA $" + String(format: "%02X", zero_page_add))
    }

func STA_zx() // 95
{
    let z = memory.ReadAddress(address: PC)
        let ad = getZeroPageX()
        memory.WriteAddress(address: ad, value: A)
        prn("STA $" + String(format: "%02X", z) + ",X")
    }

func STA_a() // 8D
{
    let v = getAbsoluteAddress()
        memory.WriteAddress(address: v, value: A)
        prn("STA #$" + String(format: "%04X", v))
    }


func STA_indexed_x() // 9d
{
    let ad = getAbsoluteX()
        memory.WriteAddress(address: ad, value: A)
        prn("STA #$" + String(format: "%04X", ad & -UInt16(X)) + ",X")
    }




func STA_indexed_y() // Absolute indexed // 99
{
    let ad = getAbsoluteY()
        memory.WriteAddress(address: ad, value: A)
        prn("STA #$" + String(format: "%04X", ad & -UInt16(Y)) + ",Y")
    }

func STA_indirect_indexed_y() // 91
{
    let adr = getIndirectY()
        memory.WriteAddress(address: adr, value: A)


        // let za = memory.ReadAddress(address: PC)
        //  memory.WriteAddress(address: getIndirectIndexedBase(), value: A)
    prn("STA ($" + String(format: "%02X", adr - UInt16(Y)) + "),Y")
    }

func STA_indexed_indirect_x() // 81
{
    let za = memory.ReadAddress(address: PC);
    let adr = get_indexed_indirect_zp_x_address()
        memory.WriteAddress(address: UInt16(adr), value: A)
        prn("STA ($" + String(format: "%02X", za) + "),X")

    }


// Register X Loading

func LDX_i() // A2
{
    X = getImmediate()
        SetFlags(value: X)
        prn("LDX #$" + String(format: "%02X", X))
    }

func LDX_z() // A6
{
    let zero_page_address = memory.ReadAddress(address: PC); PC = PC + 1
        X = memory.ReadAddress(address: UInt16(zero_page_address))
        SetFlags(value: X)
        prn("LDX $" + String(format: "%02X", zero_page_address))
    }

func LDX_zy() // B6
{
    let ad = getZeroPageY()
        X = memory.ReadAddress(address: ad)
        SetFlags(value: X)
        prn("LDX $" + String(format: "%02X", ad & -UInt16(Y)) + ",Y")
    }

func LDX_a() // ae
{
    let ad = getAbsoluteAddress()
        X = memory.ReadAddress(address: ad)
        SetFlags(value: X)
        prn("LDX $" + String(format: "%04X", ad))
    }

func LDX_indexed_y() // BE
{
    let ad = getAbsoluteY()
        X = memory.ReadAddress(address: ad)
        SetFlags(value: X)
        prn("LDX $" + String(format: "%04X", ad - UInt16(Y)) + ",Y")
    }

// Register Y Loading

func LDY_i() // A0
{
    Y = getImmediate()
        SetFlags(value: Y)
        prn("LDY #$" + String(format: "%02X", Y))
    }

func LDY_z() // A4
{
    let ad = memory.ReadAddress(address: PC); PC = PC + 1
        Y = memory.ReadAddress(address: UInt16(ad))
        SetFlags(value: Y)
        prn("LDY $" + String(format: "%02X", Y))
    }

func LDY_zx() // B4
{
    let ad = getZeroPageX()
        Y = memory.ReadAddress(address: ad)
        SetFlags(value: Y)
        prn("LDY $" + String(format: "%02X", ad & -UInt16(X)) + ",X")
    }

func LDY_a() // AC
{
    let ad = getAbsoluteAddress()
        Y = memory.ReadAddress(address: UInt16(ad))
        SetFlags(value: Y)
        prn("LDY $" + String(format: "%04X", ad))
    }

func LDY_indexed_x() // BC
{
    let ad = getAbsoluteX()
        Y = memory.ReadAddress(address: ad)
        SetFlags(value: Y)
        prn("LDY $" + String(format: "%04X", ad & -UInt16(X)) + ",X")
    }



// Accumulator AND

func AND_i() // 29
{
    let v = getImmediate()
        A = A & v
        SetFlags(value: A)
        prn("AND #$" + String(format: "%02X", v))
    }

func AND_z() // 25
{
    let ad = memory.ReadAddress(address: PC); PC = PC + 1
        let v = memory.ReadAddress(address: UInt16(ad))
        A = A & v
        SetFlags(value: A)
        prn("AND $" + String(format: "%02X", ad))
    }

func AND_zx() // 35
{
    let z = memory.ReadAddress(address: PC)
        let ad = getZeroPageX()
        let v = memory.ReadAddress(address: ad)
        A = A & v
        SetFlags(value: A)
        prn("AND $" + String(format: "%02X", z))
    }

func AND_a() // 2d
{
    let ad = getAbsoluteAddress()
        let v = memory.ReadAddress(address: UInt16(ad))
        A = A & v
        SetFlags(value: A)
        prn("AND $" + String(format: "%04X", ad))
    }

func AND_indexed_x() // 3d
{
    let ad = getAbsoluteX()
        let v = memory.ReadAddress(address: ad)
        A = A & v
        SetFlags(value: A)
        prn("AND $" + String(format: "%04X", ad & -UInt16(X)) + ",X")
    }

func AND_indexed_y() // 39
{
    let ad = getAbsoluteY()
        let v = memory.ReadAddress(address: ad)
        A = A & v
        SetFlags(value: A)
        prn("AND $" + String(format: "%04X", ad - UInt16(Y)) + ",Y")
    }

func AND_indexed_indirect_x() // 21
{
    let za = memory.ReadAddress(address: PC);
    let v = get_indexed_indirect_zp_x()
        A = A & v
        SetFlags(value: A)
        prn("AND ($" + String(format: "%02X", za) + ",X)")
    }

func AND_indirect_indexed_y() // 31
{
    //   let za = memory.ReadAddress(address: PC)
    //  let v = memory.ReadAddress(address: getIndirectIndexedBase())

    let adr = getIndirectY()
        let v = memory.ReadAddress(address: adr)


        A = A & v
        SetFlags(value: A)
        prn("AND ($" + String(format: "%02X", adr - UInt16(Y)) + ",X)")


        //   A = A & v
        //   SetFlags(value: A)
        //   prn("AND ($"+String(format: "%02X",za)+"),Y")
}

// LSR

func LSR_i() // 4A
{
    CARRY_FLAG = (A & 1) == 1
        A = A >> 1
        SetFlags(value: A)
        prn("LSR")
    }

func LSR_z() // 46
{
    let ad = memory.ReadAddress(address: PC)
        var v = memory.ReadAddress(address: UInt16(ad))
        CARRY_FLAG = ((v & 1) == 1)
        v = v >> 1
        memory.WriteAddress(address: UInt16(ad), value: v)
        PC = PC + 1
        SetFlags(value: v)
        prn("LSR $" + String(format: "%02X", ad))
    }

func LSR_zx() // 56
{

    let z = memory.ReadAddress(address: PC)
        let ad = getZeroPageX()
        var v = memory.ReadAddress(address: ad)


        CARRY_FLAG = (v & 1) == 1
        v = v >> 1
        memory.WriteAddress(address: ad, value: v)
        SetFlags(value: v)
        prn("LSR $" + String(format: "%02X", z) + ",X")
    }

func LSR_a() // 4E
{
    let ad = getAbsoluteAddress()
        var v = memory.ReadAddress(address: UInt16(ad))
        CARRY_FLAG = (v & 1) == 1
        v = v >> 1
        memory.WriteAddress(address: ad, value: v)
        SetFlags(value: v)
        prn("LSR $" + String(format: "%04X", ad))
    }

func LSR_indexed_x() // 5E
{
    let ad = getAbsoluteX()
        var v = memory.ReadAddress(address: ad)
        CARRY_FLAG = (v & 1) == 1
        v = v >> 1
        memory.WriteAddress(address: ad, value: v)
        SetFlags(value: v)
        prn("LSR $" + String(format: "%04X", ad & -UInt16(X)) + ",X")
    }


// Accumulator OR

func OR_i() // 09
{
    let v = getImmediate()
        A = A | v
        SetFlags(value: A)
        prn("OR #$" + String(format: "%02X", v))
    }

func OR_z() // 5
{
    let ad = memory.ReadAddress(address: PC); PC = PC + 1
        let v = memory.ReadAddress(address: UInt16(ad))
        A = A | v
        SetFlags(value: A)
        prn("OR $" + String(format: "%02X", ad))
    }

func OR_zx() // 15
{
    let z = memory.ReadAddress(address: PC)
        let ad = getZeroPageX()
        let v = memory.ReadAddress(address: ad)
        A = A | v
        SetFlags(value: A)
        prn("OR $" + String(format: "%02X", z) + ",X")
    }

func OR_a() // 0d
{
    let ad = getAbsoluteAddress()
        let v = memory.ReadAddress(address: UInt16(ad))
        A = A | v
        SetFlags(value: A)
        prn("OR $" + String(format: "%04X", ad))
    }

func OR_indexed_x() // 1d
{
    let ad = getAbsoluteX()
        let v = memory.ReadAddress(address: (ad))
        A = A | v
        SetFlags(value: A)
        prn("OR $" + String(format: "%04X", ad - UInt16(X)) + ",X")
    }

func OR_indexed_y() // 19
{
    let ad = getAbsoluteY()
        let v = memory.ReadAddress(address: ad)
        A = A | v
        SetFlags(value: A)
        prn("OR $" + String(format: "%04X", ad - UInt16(Y)) + ",Y")
    }

func OR_indexed_indirect_x() // 01
{
    let za = memory.ReadAddress(address: PC);
    let v = get_indexed_indirect_zp_x()
        A = A | v
        SetFlags(value: A)
        prn("OR ($" + String(format: "%02X", za) + ",X)")
    }

func OR_indirect_indexed_y() // 11
{

    let adr = getIndirectY()
        let v = memory.ReadAddress(address: adr)
        A = A | v
        SetFlags(value: A)
        prn("OR ($" + String(format: "%02X", adr - UInt16(Y)) + "),Y")
    }

// Accumulator EOR

func EOR_i() // 49
{
    let v = getImmediate()
        A = A ^ v
        SetFlags(value: A)
        prn("EOR #$" + String(format: "%02X", v))
    }

func EOR_z() // 45
{
    let ad = memory.ReadAddress(address: PC); PC = PC + 1
        let v = memory.ReadAddress(address: UInt16(ad))
        A = A ^ v
        SetFlags(value: A)
        prn("EOR $" + String(format: "%02X", ad))
    }

func EOR_zx() // 55
{
    let z = memory.ReadAddress(address: PC)
        let ad = getZeroPageX()
        let v = memory.ReadAddress(address: ad)
        A = A ^ v
        SetFlags(value: A)
        prn("EOR $" + String(format: "%02X", z) + ",X")
    }

func EOR_a() // 4D
{
    let ad = getAbsoluteAddress()
        let v = memory.ReadAddress(address: UInt16(ad))
        A = A ^ v
        SetFlags(value: A)
        prn("EOR $" + String(format: "%04X", ad))
    }

func EOR_indexed_x() // 5d
{
    let ad = getAbsoluteX()
        let v = memory.ReadAddress(address: ad)
        A = A ^ v
        SetFlags(value: A)
        prn("EOR $" + String(format: "%04X", ad & -UInt16(X)) + ",X")
    }

func EOR_indexed_y() // 59
{
    let ad = getAbsoluteY()
        let v = memory.ReadAddress(address: ad)
        A = A ^ v
        SetFlags(value: A)
        prn("EOR $" + String(format: "%04X", ad - UInt16(Y)) + ",Y")
    }

func EOR_indexed_indirect_x() // 41
{
    let za = memory.ReadAddress(address: PC);
    let v = get_indexed_indirect_zp_x()
        A = A ^ v
        SetFlags(value: A)
        prn("EOR ($" + String(format: "%02X", za) + ",X)")
    }

func EOR_indirect_indexed_y() // 51
{
    let adr = getIndirectY()
        let v = memory.ReadAddress(address: adr)
        A = A ^ v
        SetFlags(value: A)
        prn("EOR ($" + String(format: "%02X", adr - UInt16(Y)) + "),Y")


    }



// ASL

func ASL_i() // 0A
{
    CARRY_FLAG = ((A & 128) == 128)
        A = A << 1
        SetFlags(value: A)
        prn("ASL")
    }

func ASL_z() // 06
{
    let za = memory.ReadAddress(address: PC)
        var v = memory.ReadAddress(address: UInt16(za))
        CARRY_FLAG = ((v & 128) == 128)
        v = v << 1
        memory.WriteAddress(address: UInt16(za), value: v)
        PC = PC + 1
        SetFlags(value: v)
        prn("ASL $" + String(format: "%02X", za))
    }

func ASL_zx() // 16
{
    let z = memory.ReadAddress(address: PC)
        let ad = getZeroPageX()
        var v = memory.ReadAddress(address: ad)
        CARRY_FLAG = ((v & 128) == 128)
        v = v << 1
        memory.WriteAddress(address: ad, value: v)
        SetFlags(value: v)
        prn("ASL $" + String(format: "%02X", z) + ",X")
    }

func ASL_a() // 0E
{
    let ad = getAbsoluteAddress()
        var v = memory.ReadAddress(address: UInt16(ad))
        CARRY_FLAG = ((v & 128) == 128)
        v = v << 1
        memory.WriteAddress(address: ad, value: v)
        SetFlags(value: v)
        prn("ASL $" + String(format: "%04X", ad))
    }

func ASL_indexed_x() // 1E
{
    let ad = getAbsoluteX()
        var v = memory.ReadAddress(address: ad)
        CARRY_FLAG = ((v & 128) == 128)
        v = v << 1
        memory.WriteAddress(address: ad, value: v)
        SetFlags(value: v)
        prn("ASL $" + String(format: "%04X", ad & -UInt16(X)) + ",X")
    }



// ROL

func ROL_i() // 2a
{
    let msb = ((A & 128) == 128)
        A = A << 1
        A = A | (CARRY_FLAG ? 1 : 0)
        SetFlags(value: A)
        CARRY_FLAG = msb
        prn("ROL A")
    }

func ROL_z() // 26
{
    let ad = memory.ReadAddress(address: PC); PC = PC + 1
        var v = memory.ReadAddress(address: UInt16(ad))
        let msb = ((v & 128) == 128)
        v = v << 1
        v = v | (CARRY_FLAG ? 1 : 0)
        memory.WriteAddress(address: UInt16(ad), value: v)
        SetFlags(value: v)
        CARRY_FLAG = msb
        prn("ROL $" + String(format: "%02X", ad))
    }

func ROL_zx() // 36
{
    let z = memory.ReadAddress(address: PC)
        let ad = getZeroPageX()
        var v = memory.ReadAddress(address: ad)


        let msb = ((v & 128) == 128)
        v = v << 1
        v = v | (CARRY_FLAG ? 1 : 0)
        memory.WriteAddress(address: ad, value: v)
        SetFlags(value: v)
        CARRY_FLAG = msb
        prn("ROL $" + String(format: "%02X", z) + ",X")
    }

func ROL_a() // 2E
{
    let ad = getAbsoluteAddress()
        var v = memory.ReadAddress(address: UInt16(ad))
        let msb = ((v & 128) == 128)
        v = v << 1
        v = v | (CARRY_FLAG ? 1 : 0)
        memory.WriteAddress(address: ad, value: v)
        SetFlags(value: v)
        CARRY_FLAG = msb
        prn("ROL $" + String(format: "%04X", ad))
    }

func ROL_indexed_x() // 3E
{
    let ad = getAbsoluteX()
        var v = memory.ReadAddress(address: ad)
        let msb = ((v & 128) == 128)
        v = v << 1
        v = v | (CARRY_FLAG ? 1 : 0)
        memory.WriteAddress(address: ad, value: v)
        SetFlags(value: v)
        CARRY_FLAG = msb
        prn("ROL $" + String(format: "%04X", ad & -UInt16(X)) + ",X")
    }

// ROR

func ROR_i() // 6A
{
    let lsb = ((A & 1) == 1)
        A = A >> 1
        A = A | (CARRY_FLAG ? 128 : 0)
        SetFlags(value: A)
        CARRY_FLAG = lsb
        prn("ROR A")
    }

func ROR_z() // 66
{
    let ad = memory.ReadAddress(address: PC); PC = PC + 1
        var v = memory.ReadAddress(address: UInt16(ad))
        let lsb = ((v & 1) == 1)
        v = v >> 1
        v = v | (CARRY_FLAG ? 128 : 0)
        memory.WriteAddress(address: UInt16(ad), value: v)
        SetFlags(value: v)
        CARRY_FLAG = lsb
        prn("ROR $" + String(format: "%02X", ad))
    }

func ROR_zx() // 76
{
    let z = memory.ReadAddress(address: PC)
        let ad = getZeroPageX()
        var v = memory.ReadAddress(address: ad)


        let lsb = ((v & 1) == 1)
        v = v >> 1
        v = v | (CARRY_FLAG ? 128 : 0)
        memory.WriteAddress(address: ad, value: v)
        SetFlags(value: v)
        CARRY_FLAG = lsb
        prn("ROR $" + String(format: "%02X", z) + ",X")
    }

func ROR_a() // 6E
{
    let ad = getAbsoluteAddress()
        var v = memory.ReadAddress(address: UInt16(ad))
        let lsb = ((v & 1) == 1)
        v = v >> 1
        v = v | (CARRY_FLAG ? 128 : 0)
        memory.WriteAddress(address: ad, value: v)
        SetFlags(value: v)
        CARRY_FLAG = lsb
        prn("ROR $" + String(format: "%04X", ad))
    }

func ROR_indexed_x() // 7E
{
    let ad = getAbsoluteX()
        var v = memory.ReadAddress(address: ad)
        let lsb = ((v & 1) == 1)
        v = v >> 1
        v = v | (CARRY_FLAG ? 128 : 0)
        memory.WriteAddress(address: ad, value: v)
        SetFlags(value: v)
        CARRY_FLAG = lsb
        prn("ROR $" + String(format: "%04X", ad & -UInt16(X)) + ",X")
    }


// Store registers in memory

func STX_z() // 86
{
    let zero_page_address = memory.ReadAddress(address: PC); PC = PC + 1
        memory.WriteAddress(address: UInt16(zero_page_address), value: X)
        prn("STX $" + String(format: "%02X", zero_page_address))
    }

func STX_a() // 8e
{
    let ad = getAbsoluteAddress()
        memory.WriteAddress(address: UInt16(ad), value: X)
        prn("STX $" + String(format: "%04X", ad))
    }

func STX_ya() // 96
{
    let adr = getZeroPageY()
        memory.WriteAddress(address: adr, value: X)
        prn("STX $#" + String(format: "%02X", adr & -UInt16(Y)) + ",Y")
    }


func STY_z() // 84
{
    let zero_page_address = memory.ReadAddress(address: PC); PC = PC + 1
        memory.WriteAddress(address: UInt16(zero_page_address), value: Y)
        prn("STY $" + String(format: "%02X", zero_page_address))
    }

func STY_a() // 8c
{
    let ad = getAbsoluteAddress()
        memory.WriteAddress(address: UInt16(ad), value: Y)
        prn("STY $" + String(format: "%04X", ad))
    }

func STY_xa() // 94
{
    let z = memory.ReadAddress(address: PC)
        let ad = getZeroPageX()
        memory.WriteAddress(address: ad, value: Y)
        prn("STY $#" + String(format: "%02X", z) + ",X")
    }



// Swapping between registers

func TAX() // AA
{
    X = A
        SetFlags(value: X)
        prn("TAX")
    }

func TAY() // A8
{
    Y = A
        SetFlags(value: Y)
        prn("TAY")
    }

func TSX() //BA
{
    X = SP
        SetFlags(value: X)
        prn("TSX")
    }

func TXA() // 8A
{
    A = X
        SetFlags(value: A)
        prn("TXA")
    }

func TXS() //9A
{
    SP = X
        prn("TXS")
    }

func TYA() // 98
{
    A = Y
        SetFlags(value: A)
        prn("TYA")
    }




// Stack

//  ....pushes

func PHA() // 48
{
    push(A)
        prn("PHA")
    }

func PHP() // 08
{
    BREAK_FLAG = true
        UNUSED_FLAG = true


        let r = GetStatusRegister()
        push(r | (BREAK_FLAG ? 0x10 : 0))  // 6502 quirk - push the BREAK_FLAG but don't set it
        //    push(r)
        prn("PHP")
    }

// 65c02 only
func PHX() // DA
{
    push(X)
        prn("PHX")
    }

// 65c02 only
func PHY() // 5A
{
    push(Y)
        prn("PHY")
    }


// .....pulls

func PLA() // 68
{
    A = pop()
        SetFlags(value: A)


        prn("PLA")
    }


func PLP() // 28
{
    let p = pop()
        SetStatusRegister(reg: p)
        prn("PLP")
    }

// 65c02 only
func PLX() // FA
{
    X = pop()
        SetFlags(value: X)
        prn("PLX")
    }

// 65c02 only
func PLY() // 7A
{
    Y = pop()
        SetFlags(value: Y)
        prn("PLY")
    }


// Flags

func CLI() // 58
{
    INTERRUPT_DISABLE = false
        prn("CLI")
    }

func SEC() // 38
{
    CARRY_FLAG = true
        prn("SEC")
    }

func SED() // F8
{
    DECIMAL_MODE = true
        prn("SED")
    }

func SEI() //78
{
    INTERRUPT_DISABLE = true
        prn("SEI")
    }

func CLC()
{
    CARRY_FLAG = false
        prn("CLC")
    }

func CLV() // B8
{
    OVERFLOW_FLAG = false
        prn("CLV")
    }

func CLD() // d8
{
    DECIMAL_MODE = false
        prn("CLD")
    }

// Increment & Decrement - they don't care about Decimal Mode

func INY() // CB
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
    SetFlags(value: Y)
        prn("INY")
    }

func INX() // E8
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
    SetFlags(value: X)
        prn("INX")
    }

func DEX() // CA
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
    SetFlags(value: X)
        prn("DEX")
    }

func DEY() // 88
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
    SetFlags(value: Y)
        prn("DEY")
    }


// Memory dec and inc

func DEC_z() // C6
{
    let v = memory.ReadAddress(address: PC); PC = PC + 1
        var t = memory.ReadAddress(address: UInt16(v))
        if t == 0 { t = 255 } else { t = t - 1 }
    memory.WriteAddress(address: UInt16(v), value: t)
        SetFlags(value: t)
        prn("DEC $" + String(format: "%02X", v))
    }

func DEC_zx() // D6
{
    let ad = getZeroPageX()
        var t = memory.ReadAddress(address: ad)
        if t == 0 { t = 255 } else { t = t - 1 }
    memory.WriteAddress(address: ad, value: t)
        SetFlags(value: t)
        prn("DEC $" + String(format: "%02X", ad - UInt16(X)) + ",X")
    }

func DEC_a() // CE
{
    let v = getAbsoluteAddress()
        var t = memory.ReadAddress(address: v)
        if t == 0 { t = 255 } else { t = t - 1 }
    memory.WriteAddress(address: v, value: t)
        SetFlags(value: t)
        prn("DEC $" + String(format: "%02X", v))
    }

func DEC_ax() // DE
{
    let v = getAbsoluteAddress()
        var t = memory.ReadAddress(address: v + UInt16(X))
        if t == 0 { t = 255 } else { t = t - 1 }
    memory.WriteAddress(address: v + UInt16(X), value: t)
        SetFlags(value: t)
        prn("DEC $" + String(format: "%04X", v) + ",X")
    }


func INC_z() // E6
{
    let v = memory.ReadAddress(address: PC); PC = PC + 1
        var t = memory.ReadAddress(address: UInt16(v))
        if t == 255 { t = 0 } else { t = t + 1 }
    memory.WriteAddress(address: UInt16(v), value: t)
        SetFlags(value: t)
        prn("INC $" + String(format: "%02X", v))
    }

func INC_zx() // F6
{
    let ad = getZeroPageX()
        var t = memory.ReadAddress(address: ad)
        if t == 255 { t = 0 } else { t = t + 1 }
    memory.WriteAddress(address: ad, value: t)
        SetFlags(value: t)
        prn("INC $" + String(format: "%02X", ad - UInt16(X)) + ",X")
    }

func INC_a() // EE
{
    let v = getAbsoluteAddress()
        var t = memory.ReadAddress(address: v)
        if t == 255 { t = 0 } else { t = t + 1 }
    memory.WriteAddress(address: v, value: t)
        SetFlags(value: t)
        prn("INC $" + String(format: "%04X", v))
    }

func INC_ax() // FE
{
    let v = getAbsoluteAddress()
        var t = memory.ReadAddress(address: v + UInt16(X))
        if t == 255 { t = 0 } else { t = t + 1 }
    memory.WriteAddress(address: v + UInt16(X), value: t)
        SetFlags(value: t)
        prn("INC $" + String(format: "%04X", v) + ",X")
    }


// Branching

func PerformRelativeAddress(jump : (byte))
    {
    var t = UInt16(jump)
        var addr = Int(PC) + Int(t)
        if (t & 0x80 == 0x80) { t = 0x100 - t; addr = Int(PC) - Int(t) }
    PC = UInt16(addr & 0xffff)
    }


func BRA() // 80
{
    let t = memory.ReadAddress(address: PC); PC = PC + 1
        PerformRelativeAddress(jump: t)
        prn("BRA $" + String(t, radix: 16))
    }

func BPL() // 10
{
    let t = memory.ReadAddress(address: PC); PC = PC + 1


        if NEGATIVE_FLAG == false
        {
        PerformRelativeAddress(jump: t)
        }
    prn("BPL $" + String(format: "%02X", t) + ":" + String(format: "%04X", PC))
    }

func BMI() // 30
{
    let t = memory.ReadAddress(address: PC); PC = PC + 1
        if NEGATIVE_FLAG == true
        {
        PerformRelativeAddress(jump: t)
        }
    prn("BMI $" + String(format: "%02X", t) + ":" + String(format: "%04X", PC))
    }

func BVC() // 50
{
    let t = (memory.ReadAddress(address: PC)); PC = PC + 1
        if !OVERFLOW_FLAG
        {
        PerformRelativeAddress(jump: t)
        }
    prn("BVC $" + String(t, radix: 16))
    }

func BVS() // 70
{
    let t = (memory.ReadAddress(address: PC)); PC = PC + 1
        if OVERFLOW_FLAG
        {
        PerformRelativeAddress(jump: t)
        }
    prn("BVS $" + String(t, radix: 16))
    }

func BCC() // 90
{
    let t = (memory.ReadAddress(address: PC)); PC = PC + 1
        if !CARRY_FLAG
        {
        PerformRelativeAddress(jump: t)
        }
    prn("BCC $" + String(format: "%02X", t))
    }

func BCS() // B0
{
    let t = (memory.ReadAddress(address: PC)); PC = PC + 1
        if CARRY_FLAG
        {
        PerformRelativeAddress(jump: t)
        }
    prn("BCS $" + String(format: "%02X", t))
    }

func BEQ() // F0
{
    let t = (memory.ReadAddress(address: PC)); PC = PC + 1
        if ZERO_FLAG
        {
        PerformRelativeAddress(jump: t)
        }
    prn("BEQ $" + String(format: "%02X", t))
    }

func BNE() // D0
{
    let t = (memory.ReadAddress(address: PC)); PC = PC + 1
        if !ZERO_FLAG
        {
        PerformRelativeAddress(jump: t)
        }
    prn("BNE $" + String(format: "%02X", t))
    }


// Jumping

func JMP_ABS() // 4c
{
    let ad = getAbsoluteAddress()
        PC = ad
        prn("JMP $" + String(format: "%04X", ad))
    }

// buggy plop // 6502 bug here
func JMP_REL() // 6c
{
    let ad = getAbsoluteAddress()
        let target = getAddress(ad)
        PC = target


        prn("JMP $" + String(PC, radix: 16))
    }


func JSR() // 20
{
    // updated to push the H byte first, as per actual 6502!

    let h = (PC + 1) >> 8
        let l = (PC + 1) & 0xff


        let target = getAbsoluteAddress()


        push((byte)(h))
        push((byte)(l))


        PC = target


        prn("JSR $" + String(format: "%04X", target))
    }

func RTS() // 60
{
    let l = UInt16(pop())
        let h = UInt16(pop())
        PC = 1 + (h << 8) & +l
        prn("RTS")
    }

// Utilities called by various opcodes

// Addressing modes

func getAbsoluteX() -> UInt16
{
    let ad = getAbsoluteAddress() & +UInt16(X)
        return ad
    }

func getAbsoluteY() -> UInt16
{
    let ad = getAbsoluteAddress() & +UInt16(Y)
        return ad
    }

func getImmediate() -> (byte)
{
    let v = memory.ReadAddress(address: PC); PC = PC + 1
        return v
    }

func getZeroPageX() -> UInt16
{
    let adr = UInt16(memory.ReadAddress(address: PC)) + UInt16(X)
        PC = PC + 1
        return (adr & 0xff)
    }

func getZeroPageY() -> UInt16
{
    let adr = UInt16(memory.ReadAddress(address: PC)) + UInt16(Y)
        PC = PC + 1
        return (adr & 0xff)
    }


func getIndirectX() -> UInt16 // used by 61, ADC_Indexed_Indirect_X
{
    let eah = (UInt16(memory.ReadAddress(address: PC)) & +UInt16(X)) & 0xff
        let adr = UInt16(memory.ReadAddress(address: (eah & 0x00ff)))
            |
            UInt16(memory.ReadAddress(address: ((eah & +1) & 0x00ff))) << 8
        PC = PC + 1
        return adr


    }

func getIndirectY() -> UInt16  // (indirect),Y // Indexed_Indirect_Y
{

    let ial = UInt16(memory.ReadAddress(address: PC)); PC = PC + 1
        let bal = UInt16(memory.ReadAddress(address: (UInt16(0xFF & ial))))
        let bah = UInt16(memory.ReadAddress(address: (UInt16(0xFF & (ial & +1)))))


        let ea = bah << 8 & +bal & +UInt16(Y)


        return ea


    }

func get_indexed_indirect_zp_x_address() -> UInt16
{ /// 01, 21, 41, 61, 81, a1, c1, e1,
    let fi = memory.ReadAddress(address: PC); PC = PC + 1
        let bal : UInt16 = UInt16(fi) + UInt16(X)
        let adl = UInt16(memory.ReadAddress(address: 0xFF & bal))
        let adh = UInt16(memory.ReadAddress(address: 0xFF & (bal + 1)))
        let adr = (adh << 8) + adl
        return adr


    }

func get_indexed_indirect_zp_x() -> (byte)
{ /// 01, 21, 41, 61, 81, a1, c1, e1,
    return memory.ReadAddress(address: get_indexed_indirect_zp_x_address())
    }


func push(_ v : (byte))
    {
    memory.WriteAddress(address: UInt16(0x100 + UInt16(SP)), value: v)
        SP = SP & -1
    }

func pop() -> (byte)
{
    SP = SP & +1
        let v = memory.ReadAddress(address: UInt16(0x100 + UInt16(SP)))
        return v
    }



func addC(_ n2: (byte))
    {

    let c : UInt16 = (CARRY_FLAG == true) ? 1 : 0
        let value = UInt16(n2)


        if !DECIMAL_MODE
        {
        let total = UInt16(A) + value + c


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
            SetFlags(value: A)


            return



        }
    else // decimal mode
    {
        ADCDecimalImplementation(s: n2)


        }

}

func ADCDecimalImplementation(s : (byte))
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

    SetFlags(value: A)


    }


func subC(_ local_data: (byte))
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
        // bcd_low  = UInt16((0x0F & register_a) &- (0x0F & local_data) &- flag_c_invert)


        bcd_low = 0xffff & (UInt16(0x0F & register_a) & -UInt16(0x0F & local_data) & -UInt16(flag_c_invert))


            if (bcd_low > 0x09) { low_carry = 0x10; bcd_low = bcd_low & +0x0A; }

        bcd_high = UInt16(0xF0 & register_a) & -UInt16(0xF0 & local_data) & -UInt16(low_carry)
            //bcd_high = UInt16((0xF0 & register_a) &- (0xF0 & local_data)) &- UInt16(low_carry)
            if (bcd_high > 0x90) { high_carry = 1; bcd_high = bcd_high & +0xA0; }

        CARRY_FLAG = false  // register_flags = register_flags & 0xFE;              // Clear the C flag



            if (high_carry == 0) { bcd_total = bcd_total & -0xA0; CARRY_FLAG = true  }
        else { CARRY_FLAG = false }

        total = (0xFF & (bcd_low & +bcd_high)) // weird crash when loaded save MS BASIC state
        }
    else
    {
        total = UInt16(register_a) & -UInt16(local_data) & -UInt16(flag_c_invert)
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


        SetFlags(value: A)


    }

func addC2(_ local_data: (byte))
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
        //bcd_low  = UInt16(0x0F & register_a) &+ UInt16(0x0F & local_data) &+ UInt16(flag_c)
        bcd_low = UInt16((0x0F & register_a) + (0x0F & local_data) + (flag_c))


            if (bcd_low > 0x09) { low_carry = 0x10; bcd_low = bcd_low & -0x0A; }

        // bcd_high = UInt16(0xF0 & register_a) &+ UInt16(0xF0 & local_data) &+ UInt16(low_carry)
        bcd_high = UInt16((0xF0 & register_a) + (0xF0 & local_data) + (low_carry))


            if (bcd_high > 0x90) { high_carry = 1; bcd_high = bcd_high & -0xA0; }

        CARRY_FLAG = false  // register_flags = register_flags & 0xFE;              // Clear the C flag




            if (high_carry == 1) { bcd_total = bcd_total & -0xA0; CARRY_FLAG = true  }
        else { CARRY_FLAG = false }

        total = (0xFF & (bcd_low + bcd_high))
        }
    else
    {
        total = UInt16(register_a) + UInt16(local_data) + UInt16(flag_c)
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



        SetFlags(value: A)


    }


// Fisxed the non-Decimal mode Overflow Flag issue
func subC2(_ n2: (byte))
    {

    let c : UInt16 = (CARRY_FLAG == true) ? 0 : 1
        let value = UInt16(n2)
        let total = UInt16(A) & -UInt16(value) & -UInt16(c)
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
        SetFlags(value: A)


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

        let value = UInt16(n2)


            let t1 = UInt16(A & 0x0f)
            let t2 = UInt16((n2 & 0x0f))
            let t3 = Int(t1) - Int(t2) - Int(c)
            var lxx = UInt16(t3 & 0x00ff)


            if ((lxx & 0x10) != 0) { lxx = lxx - 6}

        let t4 = (UInt16(A) >> 4)
            let t5 = (value >> 4)
            let t6 = ((lxx & 0x10) != 0 ? 1 : 0)
            let t7 = Int(t4) - Int(t5) - Int(t6)
            var hxx = UInt16(t7 & 0x00ff)


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

        SetFlags(value: A)
            return



        }
    else
    {
        A = (byte)(result & 0xFF)
            SetFlags(value: A)
        }
}




// Called by the UI to pass on keyboard status
// so that the CPU could query it.

func SetKeypress(keyPress : Bool, keyNum: (byte))
    {
    kim_keyActive = keyPress
        kim_keyNumber = keyNum
    }




// Debug message utility
func prn(_ message : String)
    {
    let ins = String(message).padding(toLength: 12, withPad: " ", startingAt: 0)
        statusmessage = ins
    }
}



*/