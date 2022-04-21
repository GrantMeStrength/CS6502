using System;
namespace CPU6502
{
    public class Memory
    {

        struct Memory_Cell
        {
            public byte cell;
            public bool ROM;
        }


        Memory_Cell[] MEMORY = new Memory_Cell[64 * 1024];

        UInt16 MaskMemory(UInt16 address) 
        {
            return (UInt16)(address & 0xffff);
        }

        byte ReadAddress(UInt16 address)
        {
            return MEMORY[(MaskMemory(address))].cell;
    }
    
     void WriteAddress(UInt16 address, byte data)
    {
        if (!MEMORY[address].ROM)
                {
                MEMORY[address].cell = data;
            }
    }






    public Memory()
        {
           // Initialize any special Memory characteristics such as defining ROM and pre-loading programs
          

        }
    }

}



/*
 * 


//
//  Memory.swift
//  VirtualKim
//
//  Created by John Kennedy on 1/8/21.
//
// Model the memory of the computers, including making memory read-only, loading some initial code,
// and wrapping addresses in a certain range (like the KIM-1 does)
// Also contains sample apps that are loaded into memory on demand.
// Note - currently only the location of the KIM ROM produces "write errors"
// but is ignored for Apple. In practice, blocking access to memory for a ROMs doesn't make any difference
// to how things work, but it was useful when debugging.

import Foundation

struct Memory_Cell {
    var cell : UInt8
    var ROM : Bool
}

//var MODESELECT : Bool = false
 var MEMORY : [Memory_Cell] = []
private let MEMORY_TOP : UInt16 = 0x5fff // 4kb + extra 16Kb at $2000
private let MEMORY_FULL : UInt16 = 0xffff // 4kb + extra 16Kb at $2000

private var clock_divide_ratio = 1
private var clock_counter : Int = 0
private var clock_interrupt_active = false
private var clock_tick_counter = 0
private var clock_went_under_zero = false
private var prevous_clock_divide_ratio = 1

private var APPLE_MODE = false
private var apple_output_char_waiting = false
private var apple_output_char : UInt8 = 0
private var apple_key_ready = false
private var apple_key_value : UInt8 = 0

class memory_64Kb
{
    
    func MaskMemory(_ address : UInt16) -> UInt16
    {
        let add = address & 0xffff
        return add
    }
    
    func AppleActive(state : Bool)
    {
        APPLE_MODE = state
    }
    
    func AppleReady() -> Bool
    {
        return apple_key_ready
    }
    
    func AppleKeyState(state : Bool, key : UInt8)
    {
        apple_key_ready = state
        apple_key_value = key
    }
    
    func getAppleOutputState() -> (Bool, UInt8)
    {
        let a = apple_output_char_waiting
        apple_output_char_waiting = false
        return (a, apple_output_char)
    }
    
    func ReadAddress (address : UInt16) -> UInt8
    {
        // Some APPLE-1 specifics
        
        if APPLE_MODE
        {
            if address == 0xD010
            {
                if apple_key_value >= 0x61 && apple_key_value <= 0x7A
                {
                    apple_key_value = apple_key_value & 0x5f
                }
                
                if apple_key_value == 10
                {
                    apple_key_value = 13
                }
                
                apple_key_ready = false
                
                return apple_key_value | 0x80  // keypress
            }
            
            if address == 0xD011
            {
                if apple_key_ready
                {
                    return 0x80
                }
                else
                {
                    return 0x0
                }
            }
            
            if address == 0xD012 ||  address == 0xD0F2
            {
                return 0x00 // Status of keyboard. Must be zero, as in emulation 0 means "ready".
                
            }
        }
        // Some KIM-1 specifics
        
        // Cheat.. this is a RIOT timer, but here being used as a random number generator to make some apps work.
        // It's mapped to 0x17xx where xx is any number with bit 0 = 0, bit 2 = 1
        
        if address > MEMORY_TOP
        {
            if !APPLE_MODE
            {
                return 0xFF;
            }
        }
        
        let hAddr = address >> 8
        let lAddr = address & 0x00ff
        
        if (hAddr == 0x17) &&
            (lAddr & 1 == 0 && lAddr & 4 == 4)
        {
            return UInt8.random(in: 0..<255)
            
        }
        
        if address == 0x1706 {
            
            clock_interrupt_active = false
            
            if clock_went_under_zero
            {
                clock_divide_ratio = prevous_clock_divide_ratio
            }
            return UInt8(clock_counter)
        }
        
        if address == 0x170E {
            
            clock_interrupt_active = true
            
            if clock_went_under_zero
            {
                clock_divide_ratio = prevous_clock_divide_ratio
            }
            return UInt8(clock_counter)
        }
        
        
        if address == 0x1707 {
            
            if clock_went_under_zero
            {
                return 0x80
            }
            else
            {
                return 0x00
            }
        }
        
        return MEMORY[Int(MaskMemory(address))].cell
    }
    
    func WriteAddress  (address : UInt16, value : UInt8)
    {
        if address > MEMORY_TOP
        {
            if !APPLE_MODE
            {
                return
            }
        }
        
        // Some APPLE-1 specifics
        
        if (APPLE_MODE)
        {
            
            if address == 0xD011
            {
                //print("Apple:",value & 0x7F)
            }
            
            if address == 0xD012
            {
                apple_output_char_waiting = true
                apple_output_char = apple_key_value & 0x7F
            }
        }
        // KIM-1 Specifics
        
        // Clock I/O
        
        if address == 0x1704 { clock_counter = Int(value); clock_divide_ratio = 1; prevous_clock_divide_ratio = 1; clock_interrupt_active = false;  clock_went_under_zero = false; return }
        if address == 0x1705 { clock_counter = Int(value); clock_divide_ratio = 8; prevous_clock_divide_ratio = 8; clock_interrupt_active = false; clock_went_under_zero = false;return }
        if address == 0x1706 { clock_counter = Int(value); clock_divide_ratio = 64; prevous_clock_divide_ratio = 64; clock_interrupt_active = false; clock_went_under_zero = false;return }
        if address == 0x1707 { clock_counter = Int(value); clock_divide_ratio = 1024; prevous_clock_divide_ratio = 1024; clock_interrupt_active = false; clock_went_under_zero = false;return }
        if address == 0x170C { clock_counter = Int(value); clock_divide_ratio = 1; prevous_clock_divide_ratio = 1; clock_interrupt_active = true; clock_went_under_zero = false;return }
        if address == 0x170D { clock_counter = Int(value); clock_divide_ratio = 8; prevous_clock_divide_ratio = 8; clock_interrupt_active = true; clock_went_under_zero = false; return }
        if address == 0x170E { clock_counter = Int(value); clock_divide_ratio = 64; prevous_clock_divide_ratio = 64; clock_interrupt_active = true;  clock_went_under_zero = false;return}
        if address == 0x170F { clock_counter = Int(value); clock_divide_ratio = 1024; prevous_clock_divide_ratio = 1024; clock_interrupt_active = true; clock_went_under_zero = false; return}
        
        
        // Regular memory, which might also be ROM
        
        if !MEMORY[Int(MaskMemory(address))].ROM
        {
            MEMORY[Int(MaskMemory(address))].cell = value
        }
        else
        {
            if !APPLE_MODE // Only protect ROM in KIM mode
            {
            print("Trying to store " + String(format: "%02X",value) + " into ROM address " + String(format: "%04X",address) + " Wrapped to.. " + String(format: "%04X",MaskMemory(address)))
            }
        }
        
    }
    
    func RIOT_Timer_Click() // Used by KIM. Apple doesn't appear to have a Timer chip.
    {
        clock_tick_counter = clock_tick_counter + 1
        if clock_tick_counter < clock_divide_ratio { return }
        
        // clock ticker reached, so action is performed
        clock_tick_counter = 0
        clock_counter = clock_counter - 1
        
        
        if clock_counter < 0
        {
            // clock countdown reached
            clock_went_under_zero = true
            clock_divide_ratio = 1;
            MEMORY[Int(MaskMemory(0x1707))].cell = 0x80
            clock_counter = 0xff
            
        }

    }
    
    func InitMemory(SoftwareToLoad : String) -> [UInt8]
    {
        // Create RAM and Load any ROMS
        // For now, set all memory to be RAM reset to 0
        // Currently assumes software has a unique name i.e. no APPLE and KIM apps have the same name
        // Software is a mish-mash of binary files in the app bundle and built-in arrays of bytes
        // Some files will return current register settings so they can launch at run.
        
        print("Initializing memory and preparing to load \(SoftwareToLoad)..", terminator:"")
        
        MEMORY.reserveCapacity(Int(MEMORY_FULL) + 1)
        MEMORY = [Memory_Cell](repeatElement(Memory_Cell(cell: 0, ROM: false), count: Int(MEMORY_FULL) + 1))
        
        print(MEMORY.capacity, MEMORY.count, MEMORY_FULL)
        
        if !APPLE_MODE
        {
            print("KIM mode - loading KIM ROM")
            injectROM() // The Monitor ROM for KIM
            let r = LoadSoftware(name : SoftwareToLoad) // Any extra apps
            
            // Set the reset interrupt vectors to help the user
            
            // NMI - so SST/ST works
            MEMORY[0x17FA].cell = 0x00
            MEMORY[0x17FB].cell = 0x1C
            
            // RST
            MEMORY[0x17FC].cell = 0x22
            MEMORY[0x17FD].cell = 0x1C
            
            // IRQ - so BRK works
            MEMORY[0x17FE].cell = 0x00
            MEMORY[0x17FF].cell = 0x1C
            
            return r

        }
        
        else
        {
            print("Apple mode - loading WozMon")
            // Load WozMon, BASIC and Krusader into FF00, F000 and E000
            LoadAppInBinaryForm(filename: "Apple1Rom", type: "BIN", address: 0xe000)
            
            // Load in the app
            return LoadSoftware(name : SoftwareToLoad) // Any extra apps
         
        }
        
    }
    
 
    
}

*/